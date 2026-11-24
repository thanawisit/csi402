CREATE TABLE IF NOT EXISTS general_promotion_usages (
    UsageID INT NOT NULL AUTO_INCREMENT,
    PromotionID INT NOT NULL,
    UserID INT NOT NULL,
    OrderID INT NOT NULL,
    UsedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (UsageID),
    UNIQUE KEY UX_promotion_usage_order (OrderID),
    KEY IX_promotion_usage_promotion_user (PromotionID, UserID)
);

INSERT IGNORE INTO general_promotion_usages
    (PromotionID, UserID, OrderID, UsedAt)
SELECT gp.PromotionID, o.UserID, o.OrderID, o.OrderDate
FROM orders o
INNER JOIN general_promotions gp
    ON gp.Code = o.PromoCode
WHERE o.OrderStatus <> 'ยกเลิก';

UPDATE general_promotions gp
SET gp.UsedCount = (
    SELECT COUNT(*)
    FROM general_promotion_usages usage
    WHERE usage.PromotionID = gp.PromotionID
);
