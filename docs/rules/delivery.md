# Delivery and pull request rules

## Deliver one coherent result

- Give each ticket one clear outcome that can be reviewed and verified independently.
- Finish a coherent change completely, including appropriate tests and documentation.
- Judge scope by responsibilities and reviewability, not by a numerical limit on changed lines or files.
- Do not split work solely because it grew beyond an estimated size.
- Do not combine unrelated cleanup or product behavior with the ticket unless it is required for the result.

## Turn scope growth into a stack

Create a linked stack when newly discovered work contains multiple parts that can be delivered and
verified independently. Finish the current coherent change, then create a parent ticket or retain the
existing parent and order the remaining tickets beneath it.

Every stack item must:

- have its own outcome, acceptance criteria, and verification;
- identify the parent ticket;
- identify the previous and next item when they exist;
- list technical or product dependencies explicitly;
- remain safe to review and merge independently in the documented order.

Do not create a stack for implementation steps that only make sense as one atomic result. Keep those
steps in one ticket and explain the necessary scope in the pull request.

## Pull request expectations

Use the repository pull request template to record:

- the ticket and completed outcome;
- the relevant automated and manual verification;
- documentation or architecture impact;
- parent, adjacent items, and dependencies when the pull request belongs to a stack.

The stack fields are optional for an ordinary pull request. CI and review must never reject a pull
request solely because of the number of changed lines or files.

## Example: parent with two ordered items

Suppose `HOB-200` starts as "Add authenticated profile settings". During implementation, it becomes
clear that the reusable authentication boundary and the settings feature can be delivered separately.
Keep `HOB-200` as the parent and create this stack:

1. `HOB-201` / PR 1: add the authenticated-user boundary and its authorization tests.
   Parent: `HOB-200`. Previous: none. Next: `HOB-202`. Dependencies: none.
2. `HOB-202` / PR 2: add profile settings using that boundary and test ownership behavior.
   Parent: `HOB-200`. Previous: `HOB-201`. Next: none. Dependency: `HOB-201`.

Both items have a complete, testable outcome. PR 2 may depend on PR 1, but neither pull request mixes
in unrelated work or stops halfway through its own result.
