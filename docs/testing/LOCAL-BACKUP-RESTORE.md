# Local backup and restore procedure

The scripts operate only on the exact local development database `nestystay_dev` at `127.0.0.1`/`localhost`. They refuse other database names or hosts.

```powershell
.\scripts\dev-backup.ps1
.\scripts\dev-restore-test.ps1
```

`dev-backup.ps1` writes a PostgreSQL custom-format dump under `.local\dev\backups` (ignored by Git). `dev-restore-test.ps1` creates the temporary `nestystay_restore_test` database, restores the dump, verifies the migration history and removes that exact temporary database in `finally`.

This proves local dump/restore mechanics only. Production backup encryption, off-server retention, restore authorization, RPO/RTO and disaster-recovery drills still require the deployment operator.
