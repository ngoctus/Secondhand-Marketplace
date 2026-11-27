-- Migration/Seed idempotent: Bổ sung bộ từ khóa cấm Tầng 1 (HIGH + LOW) cho DB đang có dữ liệu.
-- Chạy an toàn nhiều lần: không tạo trùng, đồng thời nâng đúng mức HIGH cho các dòng "Seed ban đầu" cũ bị mặc định LOW.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

DECLARE @SeedTuKhoa TABLE (
    CumTu  nvarchar(100) NOT NULL,
    Nhom   varchar(30)   NOT NULL,
    MucDo  varchar(4)    NOT NULL,
    GhiChu nvarchar(500) NOT NULL
);

INSERT @SeedTuKhoa (CumTu, Nhom, MucDo, GhiChu) VALUES
    -- HIGH: Hàng cấm tuyệt đối (tự động từ chối AUTO_HIGH, ghi 1 vi phạm)
    (N'hàng cấm',                'HANG_CAM',          'HIGH', N'Seed ban đầu - hàng cấm'),
    (N'ma túy',                  'HANG_CAM',          'HIGH', N'Seed ban đầu - chất cấm'),
    (N'cần sa',                  'HANG_CAM',          'HIGH', N'Seed ban đầu - chất cấm'),
    (N'thuốc lắc',               'HANG_CAM',          'HIGH', N'Seed ban đầu - chất cấm'),
    (N'bóng cười',               'HANG_CAM',          'HIGH', N'Seed ban đầu - chất cấm'),
    (N'vũ khí',                  'HANG_CAM',          'HIGH', N'Seed ban đầu - vũ khí'),
    (N'súng quân dụng',          'HANG_CAM',          'HIGH', N'Seed ban đầu - vũ khí'),
    (N'súng điện',               'HANG_CAM',          'HIGH', N'Seed ban đầu - công cụ hỗ trợ cấm'),
    (N'súng hơi',                'HANG_CAM',          'HIGH', N'Seed ban đầu - vũ khí'),
    (N'dao găm',                 'HANG_CAM',          'HIGH', N'Seed ban đầu - hung khí cấm'),
    (N'dao bấm',                 'HANG_CAM',          'HIGH', N'Seed ban đầu - hung khí cấm'),
    (N'pháo nổ',                 'HANG_CAM',          'HIGH', N'Seed ban đầu - vật liệu nổ'),
    (N'thuốc nổ',                'HANG_CAM',          'HIGH', N'Seed ban đầu - vật liệu nổ'),
    (N'hàng trộm cắp',           'HANG_CAM',          'HIGH', N'Seed ban đầu - tài sản phạm pháp'),
    (N'đồ trộm cắp',             'HANG_CAM',          'HIGH', N'Seed ban đầu - tài sản phạm pháp'),
    (N'giấy tờ giả',             'HANG_CAM',          'HIGH', N'Seed ban đầu - giấy tờ giả mạo'),
    (N'bằng giả',                'HANG_CAM',          'HIGH', N'Seed ban đầu - giấy tờ giả mạo'),
    (N'căn cước giả',            'HANG_CAM',          'HIGH', N'Seed ban đầu - giấy tờ giả mạo'),
    (N'tiền giả',                'HANG_CAM',          'HIGH', N'Seed ban đầu - tiền giả'),
    (N'thuốc lá điện tử',        'HANG_CAM',          'HIGH', N'Seed ban đầu - mặt hàng cấm kinh doanh'),
    (N'pod hút',                 'HANG_CAM',          'HIGH', N'Seed ban đầu - thuốc lá điện tử'),
    (N'động vật hoang dã',       'HANG_CAM',          'HIGH', N'Seed ban đầu - động vật quý hiếm/hoang dã'),
    -- HIGH: Hàng giả / nhái trắng trợn
    (N'hàng giả',                'HANG_GIA',          'HIGH', N'Seed ban đầu - hàng giả'),
    (N'hàng nhái',               'HANG_GIA',          'HIGH', N'Seed ban đầu - hàng giả'),
    (N'hàng fake',               'HANG_GIA',          'HIGH', N'Seed ban đầu - hàng giả'),
    (N'super fake',              'HANG_GIA',          'HIGH', N'Seed ban đầu - hàng giả'),
    (N'siêu fake',               'HANG_GIA',          'HIGH', N'Seed ban đầu - hàng giả'),
    (N'fake loại 1',             'HANG_GIA',          'HIGH', N'Seed ban đầu - hàng giả'),
    -- HIGH: Lừa đảo / cờ bạc / xâm phạm tài khoản
    (N'rửa tiền',                'LUA_DAO',           'HIGH', N'Seed ban đầu - hành vi phạm pháp'),
    (N'cá độ',                   'LUA_DAO',           'HIGH', N'Seed ban đầu - cờ bạc'),
    (N'đánh bạc',                'LUA_DAO',           'HIGH', N'Seed ban đầu - cờ bạc'),
    (N'tài xỉu',                 'LUA_DAO',           'HIGH', N'Seed ban đầu - cờ bạc'),
    (N'hack tài khoản',          'LUA_DAO',           'HIGH', N'Seed ban đầu - xâm phạm tài khoản'),
    (N'bán tài khoản ngân hàng', 'LUA_DAO',           'HIGH', N'Seed ban đầu - mua bán tài khoản trái phép'),
    -- HIGH: Nội dung đồi trụy / 18+
    (N'phim sex',                'NOI_DUNG_NHAY_CAM', 'HIGH', N'Seed ban đầu - nội dung nhạy cảm'),
    (N'đồ chơi tình dục',        'NOI_DUNG_NHAY_CAM', 'HIGH', N'Seed ban đầu - mặt hàng cấm trên sàn'),
    (N'kích dục',                'NOI_DUNG_NHAY_CAM', 'HIGH', N'Seed ban đầu - mặt hàng cấm trên sàn'),

    -- LOW: Nghi vấn hàng nhái / nguồn gốc (chờ Admin kiểm tra ngữ cảnh)
    (N'replica',                 'HANG_GIA',          'LOW',  N'Seed ban đầu - cần Admin kiểm tra ngữ cảnh'),
    (N'rep 1:1',                 'HANG_GIA',          'LOW',  N'Seed ban đầu - nghi vấn hàng nhái'),
    (N'siêu cấp',                'HANG_GIA',          'LOW',  N'Seed ban đầu - nghi vấn hàng nhái'),
    (N'like auth',               'HANG_GIA',          'LOW',  N'Seed ban đầu - nghi vấn hàng nhái'),
    (N'hàng xuất dư',            'HANG_GIA',          'LOW',  N'Seed ban đầu - cần kiểm tra nguồn gốc'),
    (N'hàng tuồn',               'HANG_GIA',          'LOW',  N'Seed ban đầu - cần kiểm tra nguồn gốc'),
    (N'không rõ nguồn gốc',      'HANG_GIA',          'LOW',  N'Seed ban đầu - cần kiểm tra nguồn gốc'),
    -- LOW: Dấu hiệu rủi ro lừa đảo / đặt cọc
    (N'chuyển khoản trước',      'LUA_DAO',           'LOW',  N'Seed ban đầu - cảnh báo rủi ro chuyển tiền trước'),
    (N'đặt cọc trước',           'LUA_DAO',           'LOW',  N'Seed ban đầu - cảnh báo rủi ro đặt cọc'),
    (N'cọc trước',               'LUA_DAO',           'LOW',  N'Seed ban đầu - cảnh báo rủi ro đặt cọc'),
    (N'trúng thưởng',            'LUA_DAO',           'LOW',  N'Seed ban đầu - cảnh báo nội dung dụ dỗ'),
    (N'tặng miễn phí chỉ trả ship', 'LUA_DAO',        'LOW',  N'Seed ban đầu - cảnh báo chiêu trò thu phí ship'),
    (N'kiếm tiền nhanh',         'LUA_DAO',           'LOW',  N'Seed ban đầu - cảnh báo lừa đảo'),
    (N'lãi suất cao',            'LUA_DAO',           'LOW',  N'Seed ban đầu - cảnh báo tín dụng/đầu tư'),
    -- LOW: Kéo giao dịch ngoài sàn
    (N'nhắn zalo',               'NGOAI_SAN',         'LOW',  N'Seed ban đầu - kéo giao dịch ngoài sàn'),
    (N'kb zalo',                 'NGOAI_SAN',         'LOW',  N'Seed ban đầu - kéo giao dịch ngoài sàn'),
    (N'kết bạn zalo',            'NGOAI_SAN',         'LOW',  N'Seed ban đầu - kéo giao dịch ngoài sàn'),
    (N'ib zalo',                 'NGOAI_SAN',         'LOW',  N'Seed ban đầu - kéo giao dịch ngoài sàn'),
    (N'giao dịch ngoài',         'NGOAI_SAN',         'LOW',  N'Seed ban đầu - kéo giao dịch ngoài sàn'),
    (N'liên hệ tele',            'NGOAI_SAN',         'LOW',  N'Seed ban đầu - kéo giao dịch ngoài sàn'),
    (N'nhắn telegram',           'NGOAI_SAN',         'LOW',  N'Seed ban đầu - kéo giao dịch ngoài sàn'),
    -- LOW: Tin rác / quảng cáo không phù hợp
    (N'cho vay nóng',            'SPAM',              'LOW',  N'Seed ban đầu - tín dụng đen / spam'),
    (N'vay tiền nhanh',          'SPAM',              'LOW',  N'Seed ban đầu - quảng cáo vay tiền'),
    (N'tuyển ctv',               'SPAM',              'LOW',  N'Seed ban đầu - tin tuyển dụng không phải sản phẩm'),
    (N'việc nhẹ lương cao',      'SPAM',              'LOW',  N'Seed ban đầu - tin rác / lừa đảo');

-- Đồng bộ lại mức độ cho các dòng seed cũ (trước đây thiếu cột MucDo nên bị mặc định LOW).
UPDATE k
SET k.MucDo = v.MucDo,
    k.Nhom  = v.Nhom
FROM dbo.TuKhoaCam k
JOIN @SeedTuKhoa v ON k.CumTuChuanHoa = dbo.fn_ChuanHoaChuoi(v.CumTu)
WHERE k.GhiChu = N'Seed ban đầu';

INSERT dbo.TuKhoaCam (CumTu, CumTuChuanHoa, Nhom, MucDo, GhiChu)
SELECT v.CumTu, dbo.fn_ChuanHoaChuoi(v.CumTu), v.Nhom, v.MucDo, v.GhiChu
FROM @SeedTuKhoa v
WHERE NOT EXISTS (SELECT 1 FROM dbo.TuKhoaCam k WHERE k.CumTuChuanHoa = dbo.fn_ChuanHoaChuoi(v.CumTu));

COMMIT TRANSACTION;
GO

SELECT MucDo, COUNT(*) AS SoTuKhoa
FROM dbo.TuKhoaCam
WHERE DangApDung = 1
GROUP BY MucDo
ORDER BY MucDo;
GO
