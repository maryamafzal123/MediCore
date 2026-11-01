"""The MediCore assistant: a LangGraph tool-calling agent (ReAct loop).

   user msg ─► LLM ─► needs data? ─yes─► call tool(s) ─► LLM again ─► ... ─► final answer
                          └─no──────────────────────────────────────────────► final answer

Memory: .NET sends the last N messages of this conversation as `history`,
so the agent itself stays stateless and easy to scale.
"""
from datetime import datetime

from langchain.agents import create_agent
from langchain.chat_models import init_chat_model
from langchain_core.messages import AIMessage, HumanMessage, ToolMessage

from .api_client import HOSPITAL_TZ, MediCoreApi
from .config import LLM_MODEL
from .tools import build_tools

_model = None


def get_model():
    global _model
    if _model is None:
        _model = init_chat_model(LLM_MODEL, temperature=0.2)
    return _model


ROLE_GUIDE = {
    "Patient": "You help the patient find doctors, check free slots, book or cancel appointments, "
               "and understand their own appointments and prescriptions.",
    "Doctor": "You help the doctor review their schedule, look up their patients, update appointment status, "
              "and review prescriptions they wrote.",
    "Admin": "You help the hospital admin with statistics, searching appointments, doctors and patients.",
}


def system_prompt(name: str, role: str) -> str:
    now = datetime.now(HOSPITAL_TZ)
    return f"""You are MediCore Assistant, the AI assistant of MediCore Hospital (Lahore, Pakistan).
You are talking to {name or 'a user'} (role: {role}). {ROLE_GUIDE.get(role, '')}

Current hospital date/time: {now:%A, %d %B %Y, %H:%M} (Pakistan time). Resolve words like
"today", "tomorrow", "next Monday" from this date. Use YYYY-MM-DD and 24h HH:MM in tool calls.

RULES
1. Use tools for any real data (doctors, slots, appointments, prescriptions, stats). Never invent data, ids or times.
2. For hospital policies, timings, fees, departments, emergency or how-to questions, call search_hospital_info
   and answer only from what it returns. If it has nothing, say you don't know and suggest calling reception.
3. Before ANY action that changes data (book, cancel, status change) show a short summary
   (doctor, date, time, fee) and ask the user to confirm. Act only after a clear "yes".
4. To book: find the doctor (list_doctors) -> check_doctor_availability -> offer a few free times -> confirm -> book.
5. Medical safety: you are not a doctor. Never diagnose or prescribe or change doses. For symptoms, suggest the right
   specialization and offer to book. For emergencies (chest pain, breathing difficulty, heavy bleeding, unconsciousness,
   stroke signs) tell them immediately to call Rescue 1122 or go to the Emergency department, which is open 24/7.
6. Only discuss the current user's own data; if a tool returns a permission error, explain it politely.
7. Reply in the user's language and script (English, Urdu, or Roman Urdu). Keep replies short and clear;
   use bullet points or a small table for lists. Show times as 12-hour (e.g. 11:30 AM). Fees are in PKR.
   Appointment ids may be shown for reference."""


def _to_messages(history: list[dict]) -> list:
    msgs = []
    for item in history:
        if item["role"] == "user":
            msgs.append(HumanMessage(item["content"]))
        elif item["role"] == "assistant":
            msgs.append(AIMessage(item["content"]))
    return msgs


async def run_agent(message: str, history: list[dict], user: dict, token: str) -> dict:
    api = MediCoreApi(token)
    try:
        role = user.get("role", "")
        agent = create_agent(
            model=get_model(),
            tools=build_tools(api, role),
            system_prompt=system_prompt(user.get("name", ""), role),
        )
        result = await agent.ainvoke(
            {"messages": _to_messages(history) + [HumanMessage(message)]},
            config={"recursion_limit": 16},
        )
        new_messages = result["messages"][len(history) + 1:]
        tools_used = list(dict.fromkeys(m.name for m in new_messages if isinstance(m, ToolMessage)))
        reply = result["messages"][-1].content
        if isinstance(reply, list):  # some providers return content blocks
            reply = "".join(part.get("text", "") for part in reply if isinstance(part, dict))
        return {"reply": reply or "Sorry, I couldn't generate a reply.", "tools_used": tools_used}
    finally:
        await api.close()
