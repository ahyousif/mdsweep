# Issue tracker: GitHub

Issues and specifications for this repository live in GitHub Issues under `ahyousif/mdsweep`. Use the `gh` CLI for issue operations and infer the repository from the local Git remote.

Accepted MVP issues are added to [GitHub user project 3](https://github.com/users/ahyousif/projects/3/views/1). The project organizes work; the linked repository issue remains the source of its requirements and discussion.

## MVP conventions

- Create only buildable vertical slices or concrete bugs. Do not create administrative placeholder issues.
- Planned issues produced from the specification are already ready for implementation and bypass triage.
- Leave issues unestimated unless the two developers encounter a real planning need.
- Use the project's existing status workflow for progress. Add fields, milestones, or labels only when they solve an observed coordination problem.
- Link each implementation pull request to its issue.

## Operations

- **Create:** `gh issue create --title "..." --body "..."`
- **Read:** `gh issue view <number> --comments`
- **List:** `gh issue list --state open`
- **Comment:** `gh issue comment <number> --body "..."`
- **Label:** `gh issue edit <number> --add-label "..."` or `--remove-label "..."`
- **Close:** `gh issue close <number> --comment "..."`

## Pull requests as a triage surface

**PRs as a request surface: no.** Pull requests created by the team implement planned issues; external pull requests do not enter the issue-triage workflow automatically.

## Publishing and retrieval

When a skill says to publish to the issue tracker, create a GitHub issue and add accepted MVP work to project 3. When it says to fetch a ticket, read the issue body, comments, labels, dependencies, and linked pull requests.

Use GitHub-native issue dependencies when supported. Otherwise put `Blocked by: #<number>` at the top of the blocked issue and keep it accurate.
