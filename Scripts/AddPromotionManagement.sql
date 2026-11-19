-- Run once against the application's MySQL database.
CREATE TABLE IF NOT EXISTS home_promotions (
    PromotionID INT NOT NULL AUTO_INCREMENT,
    SlotKey VARCHAR(40) NOT NULL,
    Kicker VARCHAR(100) NOT NULL,
    Headline VARCHAR(200) NOT NULL,
    Description VARCHAR(500) NOT NULL,
    LinkText VARCHAR(100) NOT NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 1,
    SortOrder INT NOT NULL DEFAULT 0,
    PRIMARY KEY (PromotionID),
    UNIQUE KEY UX_home_promotions_SlotKey (SlotKey)
);

INSERT IGNORE INTO home_promotions
    (SlotKey, Kicker, Headline, Description, LinkText, IsActive, SortOrder)
VALUES
    ('membership', 'สิทธิพิเศษสำหรับสมาชิก', CONCAT('สมัครสมาชิก', CHAR(10), 'รับชามะลิฟรี'),
     'ร่วมเป็นสมาชิกอีสานเด้อเพื่อรับสิทธิพิเศษ', 'สมัครสมาชิก', 1, 1),
    ('buffet', 'โปรวันพิเศษ', CONCAT('เด็กอายุไม่เกิน 10 ปี', CHAR(10), 'กินฟรีเมื่อแต่งกายตามธีม'),
     'รับโค้ดส่วนลดบุฟเฟต์จากร้าน แล้วกรอกก่อนชำระเงิน', 'ดูแพ็กเกจบุฟเฟต์', 1, 2);

CREATE TABLE IF NOT EXISTS buffet_child_promotions (
    PromotionID TINYINT UNSIGNED NOT NULL,
    Code VARCHAR(50) NULL,
    StartDate DATE NULL,
    EndDate DATE NULL,
    IsActive TINYINT(1) NOT NULL DEFAULT 0,
    PRIMARY KEY (PromotionID)
);

INSERT IGNORE INTO buffet_child_promotions (PromotionID, Code, IsActive)
VALUES (1, NULL, 0);

ALTER TABLE orders
    ADD COLUMN BuffetFreeChildCount INT NOT NULL DEFAULT 0,
    ADD COLUMN PromoCode VARCHAR(50) NULL,
    ADD COLUMN DiscountAmount DECIMAL(10,2) NOT NULL DEFAULT 0.00;
