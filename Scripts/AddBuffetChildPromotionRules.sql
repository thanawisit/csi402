-- Run once against the application's MySQL/TiDB database.
ALTER TABLE buffet_child_promotions
    ADD COLUMN MaxFreeChildren INT NOT NULL DEFAULT 1;

ALTER TABLE buffet_child_promotions
    ADD COLUMN MaxChildAge INT NOT NULL DEFAULT 10;

ALTER TABLE buffet_child_promotions
    ADD COLUMN MaxChildHeightCm INT NOT NULL DEFAULT 120;
