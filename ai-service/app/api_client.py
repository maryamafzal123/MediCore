"""Thin HTTP client for the MediCore .NET API.

Every call carries the *logged-in user's* JWT, so the .NET role rules
(Patient / Doctor / Admin) still decide what the AI is allowed to see or do.
"""
from datetime import datetime, timedelta, timezone

import httpx

from .config import HOSPITAL_UTC_OFFSET_HOURS, MEDICORE_API_URL

HOSPITAL_TZ = timezone(timedelta(hours=HOSPITAL_UTC_OFFSET_HOURS))


class ApiError(Exception):
    pass


def to_local(iso_utc: str) -> str:
    """'2026-11-02T06:00:00Z' -> '2026-11-02 11:00' (hospital time)."""
    dt = datetime.fromisoformat(iso_utc.replace("Z", "+00:00"))
    if dt.tzinfo is None:
        dt = dt.replace(tzinfo=timezone.utc)
    return dt.astimezone(HOSPITAL_TZ).strftime("%Y-%m-%d %H:%M")


def local_to_utc_iso(date: str, time: str) -> str:
    """('2026-11-02', '11:00') hospital time -> '2026-11-02T06:00:00Z'."""
    local = datetime.strptime(f"{date} {time}", "%Y-%m-%d %H:%M").replace(tzinfo=HOSPITAL_TZ)
    return local.astimezone(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


class MediCoreApi:
    def __init__(self, token: str):
        self._client = httpx.AsyncClient(
            base_url=MEDICORE_API_URL,
            headers={"Authorization": f"Bearer {token}"},
            timeout=20,
        )

    async def request(self, method: str, path: str, **kwargs):
        try:
            response = await self._client.request(method, path, **kwargs)
        except httpx.HTTPError as exc:
            raise ApiError(f"Could not reach the hospital system: {exc}") from exc

        if response.status_code == 403:
            raise ApiError("You are not allowed to do this with your account role.")
        if response.status_code == 401:
            raise ApiError("Your session has expired. Please log in again.")
        if response.status_code >= 400:
            raise ApiError(response.text.strip('"') or f"Request failed ({response.status_code})")

        try:
            return response.json()
        except ValueError:
            return response.text.strip('"')

    async def close(self):
        await self._client.aclose()
