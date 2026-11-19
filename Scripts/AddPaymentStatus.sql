-- Run once against the application's MySQL database.
ALTER TABLE orders
    ADD COLUMN PaymentStatus VARCHAR(30) NOT NULL DEFAULT 'รอชำระเงิน';
