# Contributing to This Project

Thanks for wanting to help — I really appreciate it!  
This document explains how to download, edit, and submit changes so I can review and merge them.

---

## Summary of Permissions

You are free to **download, use, and edit** the code to prepare improvements or bug fixes for submission to this repository.

You are **not allowed** to publicly redistribute, republish, or sell modified versions without written permission.  
See `LICENSE.md` for full details.

By submitting any contribution (issue, pull request, patch, or file), you confirm that:

- You have the right to submit it.
- You grant the project owner a perpetual, worldwide, royalty-free licence to use, modify, distribute, and sublicense your contribution as part of the project.

---

## How to Contribute

1. **Fork** this repository on GitHub (top-right of the repo page).

2. **Clone** your fork locally:
    ```bash
    git clone https://github.com/your-username/THIS-REPO.git
    cd THIS-REPO
    ```

3. **Create a branch** for your change:
    ```bash
    git checkout -b fix/my-bug-or-feature
    ```

4. Make your changes locally. Keep commits small and focused.

5. **Commit** with clear messages:
    ```bash
    git add .
    git commit -m "Short summary of change"
    ```

6. **Push** your branch:
    ```bash
    git push origin fix/my-bug-or-feature
    ```

7. Open a **Pull Request** from your branch into the main repository’s `main` branch.  
   Use the pull request template if one is available.

---

## Pull Request Checklist

Before opening a PR, please ensure:

- [ ] The change focuses on one thing (bugfix, feature, or documentation).
- [ ] The project builds and runs correctly (for Unity: open the project and check the console for errors).
- [ ] Any relevant documentation or README sections are updated.
- [ ] Large files or binary assets are avoided where possible.
- [ ] The PR description clearly explains what the change does and why.

---

## Coding Style and Project Notes

- Keep file and folder names descriptive.
- Avoid committing automatically generated folders like `Library/`, `Temp/`, `.vs/`, or build outputs.  
  (These should already be excluded by `.gitignore`.)
- For large assets, use Git LFS and mention this in your PR.
- Add comments for non-obvious code and document any public API changes.

---

## Testing

- Describe how you tested your changes in the PR (what scenes, what steps).  
- If the change is non-trivial, include reproduction steps for bugs or steps to verify the feature works.

---

## Reporting Issues or Suggesting Features

- Use the **Issues** tab to report bugs or suggest features.
- Include:
  - Clear reproduction steps  
  - Unity version (if relevant)  
  - Platform (Windows, macOS, etc.)  
  - Any error messages, screenshots, or logs if possible

---

## Licence and Contribution Rights

- Contributions are accepted under the terms set out in `LICENSE.md`.  
- By submitting a contribution, you agree that:
  - You have the right to submit it.  
  - You grant the Owner a licence to use it as part of the project.  
- The Owner reserves the right to accept, reject, or modify contributions.  
- The Owner may revoke permission for future distribution per `LICENSE.md`.

---

## Communication and Help

- If you’re unsure about a change, open an Issue first to discuss it.  
- For large or breaking changes, please discuss before opening a PR.  

---

## Thank You

Thanks again for helping improve this project.  
All meaningful contributions will be credited appropriately in the project history or release notes.