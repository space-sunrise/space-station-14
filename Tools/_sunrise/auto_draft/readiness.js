// Проверки читаются штатным токеном Actions; приложению не нужны дополнительные права.
module.exports = async ({ github, owner, repo, pullRequest, rulesCache }) => {
  const checks = [];
  let cursor = null;
  do {
    const result = await github.graphql(
      `query Readiness($owner: String!, $repo: String!, $number: Int!, $cursor: String) {
        repository(owner: $owner, name: $repo) {
          pullRequest(number: $number) {
            headRefOid
            baseRefName
            commits(last: 1) {
              nodes { commit { statusCheckRollup {
                contexts(first: 100, after: $cursor) {
                  pageInfo { hasNextPage endCursor }
                  nodes {
                    __typename
                    ... on CheckRun {
                      databaseId name status conclusion title
                      isRequired(pullRequestNumber: $number)
                      checkSuite { app { databaseId slug } workflowRun { workflow { databaseId } } }
                    }
                    ... on StatusContext {
                      context state description createdAt
                      creator { login __typename }
                      isRequired(pullRequestNumber: $number)
                    }
                  }
                }
              } } }
            }
          }
        }
      }`,
      { owner, repo, number: pullRequest.number, cursor },
    );
    const current = result.repository.pullRequest;
    if (!current || current.headRefOid !== pullRequest.headRefOid || current.baseRefName !== pullRequest.baseRefName)
      throw new Error('ПР изменился во время чтения проверок; требуется повторная синхронизация.');
    const connection = current.commits.nodes[0]?.commit.statusCheckRollup?.contexts;
    checks.push(...(connection?.nodes || []));
    cursor = connection?.pageInfo.hasNextPage ? connection.pageInfo.endCursor : null;
  } while (cursor);

  // Повторный запуск заменяет старый результат, но одноимённые задания разных workflow независимы.
  const latest = new Map();
  for (const check of checks) {
    const isRun = check.__typename === 'CheckRun';
    const key = isRun
      ? `run:${check.checkSuite.app?.databaseId}:${check.checkSuite.workflowRun?.workflow.databaseId}:${check.name}`
      : `status:${check.context}`;
    const previous = latest.get(key);
    const order = isRun ? check.databaseId : Date.parse(check.createdAt);
    const previousOrder = previous?.__typename === 'CheckRun'
      ? previous.databaseId : Date.parse(previous?.createdAt);
    if (!previous || order > previousOrder)
      latest.set(key, check);
  }
  const currentChecks = [...latest.values()];
  const isRabbit = check => check.__typename === 'StatusContext'
    ? check.context === 'CodeRabbit' && check.creator?.__typename === 'Bot' && check.creator.login === 'coderabbitai'
    : check.checkSuite.app?.slug === 'coderabbitai';
  const rabbitChecks = currentChecks.filter(isRabbit);
  const succeeded = check => check.__typename === 'StatusContext'
    ? check.state === 'SUCCESS'
    : check.status === 'COMPLETED' && ['SUCCESS', 'NEUTRAL', 'SKIPPED'].includes(check.conclusion);
  const reviewed = rabbitChecks.some(check => succeeded(check) &&
    /^Review completed\b/i.test(check.description || check.title || ''));

  let rateLimited = false;
  if (!reviewed && process.env.AUTO_DRAFT_ALLOW_CODERABBIT_RATE_LIMIT !== 'false') {
    const comments = await github.paginate(github.rest.issues.listComments, {
      owner, repo, issue_number: pullRequest.number, per_page: 100,
    });
    rateLimited = comments.some(comment => {
      if (comment.user?.type !== 'Bot' || comment.user.login !== 'coderabbitai[bot]')
        return false;
      // Проверяем служебный блок, а не упоминание лимита в произвольном комментарии.
      const notice = comment.body?.trim().match(
        /^(?:<!-- This is an auto-generated comment: summarize by coderabbit\.ai -->\s*)?<!-- This is an auto-generated comment: rate limited by coderabbit\.ai -->([\s\S]*?)<!-- end of auto-generated comment: rate limited by coderabbit\.ai -->/,
      );
      const head = notice?.[1].match(/Reviewing files[^\r\n]*\bbetween\s+`?[a-f0-9]{7,40}`?\s+and\s+`?([a-f0-9]{7,40})`?\b/i)?.[1];
      if (!head || !pullRequest.headRefOid.toLowerCase().startsWith(head.toLowerCase()))
        return false;
      // Если кролик уже начал новый проход, старое сообщение о лимите не заменяет его результат.
      return !rabbitChecks.some(check => check.state === 'PENDING' &&
        Date.parse(check.createdAt) > Date.parse(comment.updated_at));
    });
  }
  const codeRabbitReady = reviewed || rateLimited;

  if (!rulesCache.has(pullRequest.baseRefName)) {
    const rules = await github.paginate('GET /repos/{owner}/{repo}/rules/branches/{branch}', {
      owner, repo, branch: pullRequest.baseRefName, per_page: 100,
    });
    rulesCache.set(pullRequest.baseRefName, rules);
  }
  // Сводка защиты ветки доступна с Contents: read; поле GraphQL требует прав администратора.
  const classicKey = `classic:${pullRequest.baseRefName}`;
  if (!rulesCache.has(classicKey)) {
    const { data: branch } = await github.rest.repos.getBranch({ owner, repo, branch: pullRequest.baseRefName });
    const required = branch.protection?.required_status_checks;
    rulesCache.set(classicKey, branch.protection?.enabled === false || required?.enforcement_level === 'off'
      ? [] : (required?.checks?.length ? required.checks : required?.contexts?.map(context => ({ context })) || []));
  }
  const requirements = rulesCache.get(classicKey).map(check => ({ context: check.context, integration_id: check.app_id }));
  const workflows = [];
  for (const rule of rulesCache.get(pullRequest.baseRefName)) {
    if (rule.type === 'required_status_checks')
      requirements.push(...rule.parameters.required_status_checks);
    if (rule.type === 'workflows')
      workflows.push(...rule.parameters.workflows);
  }
  const requiredChecks = currentChecks.filter(check => check.isRequired);
  const missingChecks = requirements.filter(requirement => !requiredChecks.some(check =>
    (check.name || check.context) === requirement.context &&
    (requirement.integration_id == null || requirement.integration_id === -1 ||
      check.__typename === 'StatusContext' || check.checkSuite.app?.databaseId === requirement.integration_id)));
  const checkItems = [
    ...missingChecks.map(check => ({ name: check.context, done: false })),
    ...requiredChecks.map(check => ({ name: check.name || check.context,
      done: isRabbit(check) ? codeRabbitReady : succeeded(check) })),
  ];
  if (workflows.length > 0) {
    const runs = await github.paginate(github.rest.actions.listWorkflowRunsForRepo, {
      owner, repo, head_sha: pullRequest.headRefOid, per_page: 100,
    });
    for (const workflow of workflows) {
      const sourceKey = `workflow-repository:${workflow.repository_id}`;
      if (!rulesCache.has(sourceKey)) {
        const { data: source } = await github.request('GET /repositories/{repository_id}', {
          repository_id: workflow.repository_id,
        });
        rulesCache.set(sourceKey, source);
      }
      const source = rulesCache.get(sourceKey);
      const sourcePath = `${source.full_name}/${workflow.path}`;
      const version = workflow.sha || workflow.ref || `refs/heads/${source.default_branch}`;
      const versions = new Set([version, version.replace(/^refs\/(heads|tags)\//, '')]);
      const matching = runs.filter(run => {
        if (run.head_sha !== pullRequest.headRefOid ||
            !['pull_request', 'pull_request_target', 'merge_group'].includes(run.event) ||
            !run.pull_requests?.some(pr => pr.number === pullRequest.number))
          return false;
        // Вызванная зависимость из referenced_workflows не доказывает запуск обязательного сценария.
        const [path, ref] = (run.path || '').split('@');
        return versions.has(ref) && (path === sourcePath ||
          run.repository?.id === workflow.repository_id && path === workflow.path);
      }).sort((a, b) => b.id - a.id);
      const latestRun = matching[0];
      checkItems.push({ name: `Сценарий ${workflow.path}`,
        done: latestRun?.status === 'completed' && latestRun.conclusion === 'success' });
    }
  }
  return {
    checksReady: checkItems.every(item => item.done),
    codeRabbitReady,
    rateLimited,
    pendingChecks: checkItems.filter(item => !item.done).map(item => item.name),
    checkItems,
  };
};
