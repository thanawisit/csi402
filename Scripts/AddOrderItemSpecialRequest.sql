-- Run once against the application's MySQL/TiDB database.
ALTER TABLE order_items
    ADD COLUMN SpecialRequest VARCHAR(500) NULL;
