# Claude Agents & Skills Guide

Reference for using Claude agents and skills in this project.

---

## Available Agents

### General Purpose
| Agent | Best For |
|---|---|
| `claude` | Catch-all — any task that doesn't fit a more specific agent (full tool access) |
| `general-purpose` | Multi-step research, open-ended code search across the codebase |

### Engineering
| Agent | Best For |
|---|---|
| `cs-senior-engineer` | Architecture decisions, code review, DevOps, API design |
| `engineering-lead` | Cross-functional coordination, tech stack evaluation, incident response |

### Product & Planning
| Agent | Best For |
|---|---|
| `agile-product-owner` | Epic breakdown, sprint planning, backlog refinement, INVEST-compliant user stories |
| `product-manager` | Feature prioritization, customer discovery, PRD development, roadmap planning |

### Specialized
| Agent | Best For |
|---|---|
| `Explore` | Fast read-only codebase search — files, symbols, keywords |
| `Plan` | Implementation planning and architecture trade-off analysis |
| `claude-code-guide` | Questions about Claude Code CLI, Agent SDK, Anthropic API |
| `statusline-setup` | Configure the Claude Code status line |

---

## Available Skills (Slash Commands)

Type `/` in Claude Code chat to see all available skills.

| Skill | Command | When to Use |
|---|---|---|
| Code Review | `/code-review` | Review current diff for bugs and quality issues |
| Ultra Review | `/code-review ultra` | Deep multi-agent cloud review before major merges |
| Run App | `/run` | Launch the app and observe behavior in browser |
| Verify Fix | `/verify` | Confirm a bug fix actually works end-to-end |
| Simplify | `/simplify` | Apply cleanup/refactor to recently changed code |
| Database Schema | `/database-schema-designer` | Create ERD diagrams, design table relationships |
| Security Review | `/security-review` | Analyze changes for OWASP Top 10 vulnerabilities |
| API Design Review | `/api-design-reviewer` | Review REST API endpoints for design consistency |

---

## How to Invoke an Agent

Tell Claude to use a specific agent:

```
"Use the agile-product-owner agent to break down the next sprint from sprint-plan.md"
"Use the cs-senior-engineer agent to review the budget module architecture"
"Use the Explore agent to find all API endpoints in source/api"
"Use the Plan agent to design the notification system"
```

> **Note:** Agents start cold — they don't have the current conversation context automatically. Claude briefs them with relevant background before delegating.

---

## Quick Reference for This Project

| Task | Use |
|---|---|
| Plan a new sprint from sprint-plan.md | `agile-product-owner` agent |
| Design a new backend module | `Plan` agent → then `cs-senior-engineer` |
| Find where OCR queue is handled | `Explore` agent |
| Find all API endpoints | `Explore` agent |
| Review a PR before merge | `/code-review` skill |
| Deep review before a release | `/code-review ultra` skill |
| Check if a bug fix actually works | `/verify` skill |
| Design a new database table | `/database-schema-designer` skill |
| Check security of a new feature | `/security-review` skill |
| Check REST API design consistency | `/api-design-reviewer` skill |
| Evaluate a tech stack decision | `engineering-lead` agent |
| Write user stories for a feature | `agile-product-owner` agent |
