import os
from pathlib import Path

from dotenv import load_dotenv

BASE_DIR = Path(__file__).resolve().parent.parent
load_dotenv(BASE_DIR / ".env")

LLM_MODEL = os.getenv("LLM_MODEL", "groq:llama-3.3-70b-versatile")
MEDICORE_API_URL = os.getenv("MEDICORE_API_URL", "http://localhost:5136").rstrip("/")
HOSPITAL_UTC_OFFSET_HOURS = int(os.getenv("HOSPITAL_UTC_OFFSET_HOURS", "5"))
RAG_MODE = os.getenv("RAG_MODE", "embeddings")
DOCS_DIR = BASE_DIR / "docs"
