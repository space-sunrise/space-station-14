const assert = require('node:assert/strict');
const load = require('../auto_draft/readiness.js');
const head = '1234567890abcdef1234567890abcdef12345678';
const pullRequest = { number: 1, headRefOid: head, baseRefName: 'master' };
const rabbit = { __typename: 'StatusContext', context: 'CodeRabbit', state: 'SUCCESS',
  description: 'Review completed', createdAt: '2026-09-01T00:00:00Z',
  creator: { __typename: 'Bot', login: 'coderabbitai' }, isRequired: false };
const check = { __typename: 'CheckRun', name: 'Tests', databaseId: 1,
  status: 'COMPLETED', conclusion: 'SUCCESS', isRequired: true,
  checkSuite: { app: { databaseId: 15368, slug: 'github-actions' },
    workflowRun: { workflow: { databaseId: 1 } } } };
const requirement = { context: 'Tests', integration_id: 15368 };
const limited = (sha = head) => ({ user: { type: 'Bot', login: 'coderabbitai[bot]' },
  updated_at: '2026-09-01T00:00:00Z',
  body: '<!-- This is an auto-generated comment: summarize by coderabbit.ai -->\n' +
    '<!-- This is an auto-generated comment: rate limited by coderabbit.ai -->\n' +
    '> ## Rate limit exceeded\n' +
    `> Reviewing files that changed from the base of the PR and between abcdef1234567 and ${sha}.\n` +
    '<!-- end of auto-generated comment: rate limited by coderabbit.ai -->',
});

async function inspect({ checks = [check, rabbit], comments = [], requirements = [requirement],
  classic = [], responseHead = head, fail = '', pages = null, rulesCache = new Map() } = {}) {
  const github = {
    rest: { issues: { listComments() {} } },
    async graphql(query, variables) {
      assert.ok(query.includes('isRequired(pullRequestNumber: $number)'));
      if (fail === 'checks') throw new Error('checks denied');
      const index = Number(variables.cursor || 0);
      const nodes = pages ? pages[index] : checks;
      return { repository: { pullRequest: { ...pullRequest, headRefOid: responseHead,
        baseRef: { branchProtectionRule: { requiredStatusChecks: classic } },
        commits: { nodes: [{ commit: { statusCheckRollup: { contexts: { nodes,
          pageInfo: { hasNextPage: pages && index + 1 < pages.length, endCursor: String(index + 1) },
        } } } }] },
      } } };
    },
    async paginate(method) {
      if (typeof method === 'string') {
        if (fail === 'rules') throw new Error('rules denied');
        return [{ type: 'required_status_checks', parameters: { required_status_checks: requirements } }];
      }
      assert.equal(method, github.rest.issues.listComments);
      return comments;
    },
  };
  return load({ github, owner: 'example', repo: 'repo', pullRequest, rulesCache });
}

(async () => {
  delete process.env.AUTO_DRAFT_ALLOW_CODERABBIT_RATE_LIMIT;
  let result = await inspect();
  assert.equal(result.checksReady, true);
  assert.equal(result.codeRabbitReady, true);

  // Отсутствующие проверки тоже блокируют готовность, включая ещё не созданные задания.
  result = await inspect({ checks: [rabbit] });
  assert.equal(result.checksReady, false);
  assert.deepEqual(result.pendingChecks, ['Tests']);
  result = await inspect({ checks: [rabbit], requirements: [],
    classic: [{ context: 'Classic CI', app: null }] });
  assert.equal(result.checksReady, false);

  for (const conclusion of ['FAILURE', 'CANCELLED', 'TIMED_OUT', null]) {
    result = await inspect({ checks: [{ ...check, conclusion }, rabbit] });
    assert.equal(result.checksReady, false);
  }
  result = await inspect({ checks: [{ ...check, status: 'IN_PROGRESS' }, rabbit] });
  assert.equal(result.checksReady, false);
  for (const conclusion of ['NEUTRAL', 'SKIPPED']) {
    result = await inspect({ checks: [{ ...check, conclusion }, rabbit] });
    assert.equal(result.checksReady, true);
  }

  // Старое падение не перекрывает успешный перезапуск; новый незавершённый запуск перекрывает успех.
  result = await inspect({ checks: [{ ...check, conclusion: 'FAILURE' }, { ...check, databaseId: 2 }, rabbit] });
  assert.equal(result.checksReady, true);
  result = await inspect({ checks: [check, { ...check, databaseId: 2, conclusion: null }, rabbit] });
  assert.equal(result.checksReady, false);
  result = await inspect({ checks: [check, { ...check, databaseId: 2, conclusion: 'FAILURE',
    checkSuite: { ...check.checkSuite, workflowRun: { workflow: { databaseId: 2 } } } }, rabbit] });
  assert.equal(result.checksReady, false);

  // Необязательная проверка и чужое приложение не заменяют обязательную проверку GitHub Actions.
  result = await inspect({ checks: [check, { ...check, name: 'Optional', isRequired: false, conclusion: 'FAILURE' }, rabbit] });
  assert.equal(result.checksReady, true);
  result = await inspect({ checks: [{ ...check, checkSuite: { app: { databaseId: 999 } } }, rabbit] });
  assert.equal(result.checksReady, false);

  result = await inspect({ checks: [check] });
  assert.equal(result.codeRabbitReady, false);
  result = await inspect({ checks: [check, { ...rabbit, state: 'PENDING' }] });
  assert.equal(result.codeRabbitReady, false);
  result = await inspect({ checks: [check, { ...rabbit, description: 'Review skipped' }] });
  assert.equal(result.codeRabbitReady, false);
  result = await inspect({ checks: [check, { ...rabbit, creator: { __typename: 'User', login: 'coderabbitai' } }] });
  assert.equal(result.codeRabbitReady, false);

  // Послабление действует только для служебного уведомления бота о текущем коммите.
  result = await inspect({ checks: [check], comments: [limited()] });
  assert.equal(result.rateLimited, true);
  assert.equal(result.codeRabbitReady, true);
  result = await inspect({ checks: [check], comments: [limited(head.slice(0, 7))] });
  assert.equal(result.rateLimited, true);
  result = await inspect({ checks: [check, { ...rabbit, description: 'Review skipped' }], comments: [limited()] });
  assert.equal(result.rateLimited, true);
  for (const comment of [limited('abcdef1234567'),
    { ...limited(), user: { type: 'User', login: 'contributor' } },
    { ...limited(), body: 'Rate limit exceeded' },
    { ...limited(), body: 'Quoted notice:\n' + limited().body },
    { ...limited(), body: limited().body.replace(/Reviewing files[^\n]+/, '') }]) {
    result = await inspect({ checks: [check], comments: [comment] });
    assert.equal(result.codeRabbitReady, false);
  }
  process.env.AUTO_DRAFT_ALLOW_CODERABBIT_RATE_LIMIT = 'false';
  result = await inspect({ checks: [check], comments: [limited()] });
  assert.equal(result.codeRabbitReady, false);
  delete process.env.AUTO_DRAFT_ALLOW_CODERABBIT_RATE_LIMIT;
  result = await inspect({ checks: [{ ...check, conclusion: 'FAILURE' }], comments: [limited()] });
  assert.equal(result.rateLimited, true);
  assert.equal(result.checksReady, false);
  result = await inspect({ checks: [check, { ...rabbit, state: 'PENDING', createdAt: '2026-09-02T00:00:00Z' }],
    comments: [limited()] });
  assert.equal(result.codeRabbitReady, false);

  // Следующая страница может содержать обязательную проверку и результат кролика.
  result = await inspect({ pages: [[], [check, rabbit]] });
  assert.equal(result.checksReady, true);
  assert.equal(result.codeRabbitReady, true);
  await assert.rejects(inspect({ responseHead: 'new-head' }), /ПР изменился/);
  await assert.rejects(inspect({ fail: 'checks' }), /checks denied/);
  await assert.rejects(inspect({ fail: 'rules' }), /rules denied/);
  console.log('Readiness scenarios passed');
})().catch(error => { console.error(error); process.exitCode = 1; });
