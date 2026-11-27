# Database migrations

These scripts upgrade an existing database. They are separate from the fresh-install bootstrap in `database/run-db.bat`.

## Current release order

For a database that already has the prior Re:Market schema, back it up and apply these scripts to the intended database in this order:

1. `202611_khieu_nai_tai_khoan.sql` — account appeal tables/columns and account-lock counters. Also adds review-response and dispute-response columns when missing.
2. `202611_danh_gia_phan_hoi_shop.sql` — standalone compatibility migration for review-response columns; it is idempotent and becomes a no-op after step 1.
3. `202611_seed_tukhoacam_high_low.sql` — synchronizes moderation keyword levels. Requires the prior moderation schema to already include `dbo.TuKhoaCam.MucDo` and `dbo.fn_ChuanHoaChuoi`.
4. `202611_payment_refund_due.sql` — adds the refund-due states and updates order-state procedures.

The scripts no longer switch databases internally. Always connect with `sqlcmd -d "%DB_NAME%"` so a custom database name is honored. Stop if the migration assumptions do not match the actual schema; do not run the fresh-install bootstrap against a database with data.

## Application database permissions

Run these schema changes using a separate deployment/migration identity. The web application's SQL identity should have only the reads, writes, and stored-procedure execution its features need; it should not have permission to create/alter tables, constraints, or procedures. The application does not apply schema DDL at startup.
