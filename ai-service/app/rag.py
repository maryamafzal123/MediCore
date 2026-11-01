"""Small RAG over the hospital handbook (docs/*.md).

Steps:  load markdown -> split by '## ' headings into chunks -> embed once at startup
        -> at question time embed the query -> cosine similarity -> top-k chunks.

If the embedding model can't be downloaded, it falls back to BM25 keyword search
so the app still works offline.
"""
import logging
import re
from dataclasses import dataclass

import numpy as np

from .config import DOCS_DIR, RAG_MODE

log = logging.getLogger(__name__)


@dataclass
class Chunk:
    source: str
    title: str
    text: str


def _load_chunks() -> list[Chunk]:
    chunks: list[Chunk] = []
    for path in sorted(DOCS_DIR.glob("*.md")):
        content = path.read_text(encoding="utf-8")
        for section in re.split(r"\n(?=## )", content):
            section = section.strip()
            if not section or section.startswith("# ") and "\n" not in section:
                continue
            title = section.splitlines()[0].lstrip("# ").strip()
            chunks.append(Chunk(source=path.name, title=title, text=section))
    return chunks


def _tokenize(text: str) -> list[str]:
    return re.findall(r"[a-z0-9]+", text.lower())


class KnowledgeBase:
    def __init__(self):
        self.chunks = _load_chunks()
        self.mode = "keyword"
        self._embedder = None
        self._matrix = None

        from rank_bm25 import BM25Okapi
        self._bm25 = BM25Okapi([_tokenize(c.text) for c in self.chunks])

        if RAG_MODE == "embeddings" and self.chunks:
            try:
                from fastembed import TextEmbedding
                self._embedder = TextEmbedding("BAAI/bge-small-en-v1.5")
                vectors = np.array(list(self._embedder.embed([c.text for c in self.chunks])))
                self._matrix = vectors / np.linalg.norm(vectors, axis=1, keepdims=True)
                self.mode = "embeddings"
            except Exception as exc:  # no internet / model download failed
                log.warning("Embeddings unavailable (%s). Using keyword search instead.", exc)

        log.info("Knowledge base ready: %d chunks, mode=%s", len(self.chunks), self.mode)

    def search(self, query: str, k: int = 3) -> list[Chunk]:
        if not self.chunks:
            return []
        if self.mode == "embeddings":
            q = np.array(list(self._embedder.query_embed(query)))[0]
            scores = self._matrix @ (q / np.linalg.norm(q))
            order = np.argsort(scores)[::-1][:k]
            return [self.chunks[i] for i in order if scores[i] > 0.3]

        scores = self._bm25.get_scores(_tokenize(query))
        order = np.argsort(scores)[::-1][:k]
        return [self.chunks[i] for i in order if scores[i] > 0]


_kb: KnowledgeBase | None = None


def get_kb() -> KnowledgeBase:
    global _kb
    if _kb is None:
        _kb = KnowledgeBase()
    return _kb
