import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from pydantic import BaseModel, Field

from .agent import run_agent
from .config import LLM_MODEL
from .rag import get_kb

logging.basicConfig(level=logging.INFO, format="%(levelname)s %(name)s: %(message)s")
log = logging.getLogger("medicore-ai")


@asynccontextmanager
async def lifespan(_: FastAPI):
    get_kb()  # load + embed the handbook once at startup
    yield


app = FastAPI(title="MediCore AI Service", lifespan=lifespan)


class HistoryItem(BaseModel):
    role: str
    content: str


class UserInfo(BaseModel):
    name: str = ""
    role: str = ""


class ChatRequest(BaseModel):
    message: str = Field(min_length=1, max_length=2000)
    history: list[HistoryItem] = []
    user: UserInfo = UserInfo()
    token: str


class ChatResponse(BaseModel):
    reply: str
    tools_used: list[str] = []


@app.get("/health")
def health():
    kb = get_kb()
    return {"status": "ok", "model": LLM_MODEL, "rag_mode": kb.mode, "chunks": len(kb.chunks)}


@app.post("/chat", response_model=ChatResponse)
async def chat(req: ChatRequest):
    try:
        result = await run_agent(
            message=req.message,
            history=[h.model_dump() for h in req.history],
            user=req.user.model_dump(),
            token=req.token,
        )
    except Exception:
        log.exception("Agent failed")
        return ChatResponse(reply="Sorry, something went wrong while processing your request. Please try again.")
    return ChatResponse(**result)
