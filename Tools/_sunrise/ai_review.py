#!/usr/bin/env python3

import os
import sys
from google import genai
from google.genai import types

API_KEY = os.environ["GEMINI_API_KEY"]
MODEL = os.environ.get("GEMINI_MODEL", "models/gemini-1.5-pro")
MAX_OUTPUT_TOKENS = int(os.environ.get("MAX_OUTPUT_TOKENS", "1500"))
TEMPERATURE = float(os.environ.get("TEMPERATURE", "0.2"))

if not API_KEY:
    print("ERROR: GEMINI_API_KEY not set", file=sys.stderr)
    sys.exit(1)

client = genai.Client(api_key=API_KEY)

def read_file(path: str) -> str:
    try:
        with open(path, "r", encoding="utf-8") as f:
            return f.read()
    except FileNotFoundError:
        return ""

def call_gemini_chat(system_prompt: str, user_prompt: str) -> str:
    contents = [
        types.Content(
            role="model",
            parts=[types.Part.from_text(text=system_prompt)],
        ),
        types.Content(
            role="user",
            parts=[types.Part.from_text(text=user_prompt)],
        ),
    ]

    generate_content_config = types.GenerateContentConfig(
        thinking_config=types.ThinkingConfig(thinking_budget=-1),
    )

    response = ""
    for chunk in client.models.generate_content_stream(
        model=MODEL,
        contents=contents,
        config=generate_content_config,
    ):
        response += chunk.text

    return response


def run_ai_review(gdd: str, diff: str, title: str, body: str) -> str:
    system_prompt = "Ты — главный геймдизайнер проекта Sunrise Station. Проанализируй Pull Request."
    user_prompt = f"""
    === TITLE ===
    {title}

    === DESCRIPTION ===
    {body}

    === GAME DESIGN DOCUMENT ===
    {gdd}

    === PULL REQUEST DIFF ===
    {diff}

    Ответь:
    1. Соответствует ли PR идеям GDD?
    2. Понижает или повышает ли баланс?
    3. Влияет ли на прогрессию отделов?
    4. Есть ли нарушения RP-принципов?
    5. Дай рекомендации.
    """
    return call_gemini_chat(system_prompt, user_prompt)

def main():
    diff = read_file("diff.txt")
    gdd = read_file(".github/gdd.md")
    title = read_file("pr_title.txt").strip()
    body = read_file("pr_body.txt").strip()

    review = run_ai_review(gdd, diff, title, body)

    with open("ai_review.txt", "w", encoding="utf-8") as f:
        f.write(review)

    print("AI review complete.")

if __name__ == "__main__":
    main()
