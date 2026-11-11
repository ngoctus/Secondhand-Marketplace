<#
.SYNOPSIS
    A2 - Chuyển danh mục hành chính (JSON nguồn) thành câu INSERT và ghi vào database/21_dia_chi_khu_vuc.sql.

.DESCRIPTION
    Nguồn: database/data/provinces_v2_depth2.json (34 tỉnh/thành, 3.321 xã/phường theo cơ cấu hành chính 2 cấp).
    Script chỉ thay phần nằm giữa hai dòng đánh dấu
        -- BEGIN GENERATED DATA
        -- END GENERATED DATA
    nên có thể chạy lại bất cứ lúc nào khi cập nhật file nguồn.
    - Mọi chuỗi tiếng Việt dùng tiền tố N'...', dấu nháy đơn được nhân đôi.
    - MaHanhChinh: tỉnh 2 chữ số (vd 79), xã/phường 5 chữ số (vd 26734).
    - INSERT ... WHERE NOT EXISTS nên chạy lại script SQL không lỗi và không nhân đôi dữ liệu.
    - Dừng với lỗi nếu: không đủ 34 tỉnh, trùng mã, xã mồ côi tỉnh, hoặc tên rỗng.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File database/tools/gen_dia_chi_insert.ps1
#>
param(
    [string]$Source = (Join-Path $PSScriptRoot '..\data\provinces_v2_depth2.json'),
    [string]$Target = (Join-Path $PSScriptRoot '..\21_dia_chi_khu_vuc.sql'),
    [int]$BatchSize = 500
)

$ErrorActionPreference = 'Stop'
$utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Esc([string]$s) { return $s.Trim().Replace("'", "''") }

$provinces = [System.IO.File]::ReadAllText((Resolve-Path $Source), $utf8NoBom) | ConvertFrom-Json

# ---- Kiểm tra dữ liệu nguồn --------------------------------------------------
if ($provinces.Count -ne 34) { throw "Phải có đúng 34 tỉnh/thành, nguồn có $($provinces.Count)." }
$provinceCodes = @{}
$wardCodes = @{}
$wardRows = New-Object System.Collections.Generic.List[object]
foreach ($p in $provinces) {
    if ([string]::IsNullOrWhiteSpace($p.name)) { throw "Tỉnh mã $($p.code) thiếu tên." }
    if ($provinceCodes.ContainsKey([int]$p.code)) { throw "Trùng mã tỉnh $($p.code)." }
    $provinceCodes[[int]$p.code] = $true
    foreach ($w in $p.wards) {
        if ([string]::IsNullOrWhiteSpace($w.name)) { throw "Xã mã $($w.code) thiếu tên." }
        if ($wardCodes.ContainsKey([int]$w.code)) { throw "Trùng mã xã/phường $($w.code)." }
        $wardCodes[[int]$w.code] = $true
        $wardRows.Add([pscustomobject]@{ Code = [int]$w.code; Province = [int]$p.code; Name = $w.name })
    }
}
foreach ($w in $wardRows) { if (-not $provinceCodes.ContainsKey($w.Province)) { throw "Xã $($w.Code) mồ côi tỉnh $($w.Province)." } }

# ---- Sinh SQL ------------------------------------------------------------------
$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine("-- Nguồn: provinces.open-api.vn (API v2, cơ cấu hành chính 2 cấp có hiệu lực từ 01/07/2025).")
[void]$sb.AppendLine("-- Phạm vi: $($provinces.Count) tỉnh/thành và $($wardRows.Count) xã/phường (đầy đủ cả nước). Không có quận/huyện.")
[void]$sb.AppendLine("-- Tệp này được sinh tự động, KHÔNG sửa tay phần giữa hai dòng BEGIN/END GENERATED DATA.")
[void]$sb.AppendLine("")

# Tỉnh/thành
[void]$sb.AppendLine("INSERT dbo.TinhThanh (MaTinhThanh, TenTinhThanh, MaHanhChinh)")
[void]$sb.AppendLine("SELECT v.Ma, v.Ten, v.MaHC")
[void]$sb.AppendLine("FROM (VALUES")
$lines = @()
foreach ($p in ($provinces | Sort-Object { [int]$_.code })) {
    $lines += ("    ({0}, N'{1}', '{2:D2}')" -f [int]$p.code, (Esc $p.name), [int]$p.code)
}
[void]$sb.AppendLine(($lines -join ",`r`n"))
[void]$sb.AppendLine(") v (Ma, Ten, MaHC)")
[void]$sb.AppendLine("WHERE NOT EXISTS (SELECT 1 FROM dbo.TinhThanh t WHERE t.MaTinhThanh = v.Ma);")
[void]$sb.AppendLine("GO")
[void]$sb.AppendLine("")

# Xã/phường theo lô
$sorted = $wardRows | Sort-Object Province, Code
for ($i = 0; $i -lt $sorted.Count; $i += $BatchSize) {
    $chunk = $sorted[$i..([Math]::Min($i + $BatchSize, $sorted.Count) - 1)]
    [void]$sb.AppendLine("INSERT dbo.PhuongXa (MaPhuongXa, MaTinhThanh, TenPhuongXa, MaHanhChinh)")
    [void]$sb.AppendLine("SELECT v.Ma, v.MaTinh, v.Ten, v.MaHC")
    [void]$sb.AppendLine("FROM (VALUES")
    $rows = foreach ($w in $chunk) { "    ({0}, {1}, N'{2}', '{3:D5}')" -f $w.Code, $w.Province, (Esc $w.Name), $w.Code }
    [void]$sb.AppendLine(($rows -join ",`r`n"))
    [void]$sb.AppendLine(") v (Ma, MaTinh, Ten, MaHC)")
    [void]$sb.AppendLine("WHERE NOT EXISTS (SELECT 1 FROM dbo.PhuongXa x WHERE x.MaPhuongXa = v.Ma);")
    [void]$sb.AppendLine("GO")
    [void]$sb.AppendLine("")
}

# ---- Ghi vào file đích giữa hai dấu --------------------------------------------
$path = (Resolve-Path $Target).Path
$text = [System.IO.File]::ReadAllText($path, $utf8NoBom)
$begin = "-- BEGIN GENERATED DATA"
$end = "-- END GENERATED DATA"
$bi = $text.IndexOf($begin); $ei = $text.IndexOf($end)
if ($bi -lt 0 -or $ei -lt 0 -or $ei -lt $bi) { throw "Không tìm thấy cặp dấu BEGIN/END GENERATED DATA trong $path." }
$new = $text.Substring(0, $bi + $begin.Length) + "`r`n" + $sb.ToString().TrimEnd() + "`r`n" + $text.Substring($ei)
[System.IO.File]::WriteAllText($path, $new, $utf8NoBom)
Write-Host "Đã ghi $($provinces.Count) tỉnh/thành và $($wardRows.Count) xã/phường vào $path"
