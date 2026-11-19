-- Run once against the application's MySQL database.
ALTER TABLE orders
    ADD COLUMN BuffetGuestCount INT NULL;
