# Publishing the v0.8.0-preview release

- [ ] Review/approve the final public reuse/distribution terms for original art, maps, text, and audio, plus third-party license obligations.
- [ ] Verify the Windows build on a clean **Windows x64** machine; check client, local server, editor and launcher.
- [ ] Test a new campaign, save/reload, main/side quests, combat, inventory, sounds, and editor export/playtest.
- [ ] Back up and try upgrading an existing Phase 7 save.
- [ ] Run `python scripts/verify_player_archive.py path/to/Greyhaven-v0.8.0-preview-Windows-x64.zip` from the source tree.
- [ ] Run `python scripts/stage_github_repo.py` after `git init` to capture upstream-ignored dependency source; upload the **source repository** to GitHub; create tag `v0.8.0-preview` on the exact matching source commit.
- [ ] Create a GitHub **pre-release** using `RELEASE_NOTES.md` and attach the Windows ZIP and SHA256SUMS file.
- [ ] Confirm screenshot links and README navigation on GitHub; optionally add repository topics, social preview image and a short About description.
- [ ] Check zipped archive via Windows Explorer extraction rather than running from inside ZIP.

**Known unverified gate:** binaries in the supplied milestone were cross-published for Windows from a Linux host. The repackaging here validates their presence and file integrity, but cannot certify actual Windows execution.
