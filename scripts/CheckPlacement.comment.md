# CheckPlacement.ps1

## How to use

1. Split the work into parts: one line per user action, and one line per piece of presentation that serves no user action.
2. Start each line with `kind=action` or `kind=presentation`, then ask which gates are open:

   ```
   checkplacement -Next -File work.txt
   ```

3. Answer every open gate on its line with one of the offered values, and repeat `-Next` until no gate stays open.
4. Ask for the plan:

   ```
   checkplacement -Plan -File work.txt
   ```

5. Read `projects`, `edges`, `tests`, `notes` and `cautions`. Treat the plan as advice, check each caution, and let the structure document decide.

A single part may go on the command line instead of a file:

```
checkplacement -Plan kind=action entry=new-element result=existing-element asks-user=no held-state=<value> rules=<value> data=none conduct-type=none text=none platform=none
```

`checkplacement -Schema` prints every gate, its values and what opens it.

```
checkplacement -Schema
checkplacement -Next  (-File <work.txt> | <token> ...)
checkplacement -Plan  (-File <work.txt> | <token> ...)
checkplacement -Help
```

| Exit code | Meaning |
|---|---|
| `0` | A plan, with or without cautions, or open gates with no input error |
| `1` | One or more input errors |
| `2` | The text file or the sidecar is missing or unreadable |

## Purpose

Suggests, before any code is written, which source projects a GUI work touches, what each carries, and what may pass between them.
It names source projects only, never a folder inside one, a type or a name.
The result is advisory; the structure document decides.

## Tokens

A token is `gate=value`, in lowercase ASCII letters, digits and hyphens, so no token needs quoting.
A gate taking several values joins them with commas: `entry=new-element,key`.
In a file, blank lines and lines starting with `#` are skipped, and the part on line n is part `pn`.
Every gate an answer opens must be answered; no gate has a default.

## Output

Every output is JSON on standard output and starts with `generation`, `project`, `stale` and `advisory`.
The header line goes to the error stream, so standard output stays pure JSON.
A plan lists `projects`, `edges`, `tests`, `new-projects`, `notes` and `cautions`.
A caution points at an answer the structure document questions and never blocks the plan.
An input error stops the plan, since the script cannot read the answer.
The JSON is written by the script's own serializer, so Windows PowerShell 5.1 and PowerShell 7 write the same bytes.

## Settings

`CheckPlacement.json` beside the script holds every project-specific value.
The script text names no project and is the same in every project.
The script hashes the structure document on every run.
It sets `stale` when the hash differs from the one recorded in `CheckPlacement.json`, or when the document is missing.

## Family

The header reads `# CHECKPLACEMENT - CHECK GENERATION 1.`
It is a planner, not a gate, so `Check.ps1` does not run it.
