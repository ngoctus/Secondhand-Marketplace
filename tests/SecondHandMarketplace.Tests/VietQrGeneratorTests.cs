using SecondHandMarketplace.Web.Common;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class VietQrGeneratorTests
{
    [Fact]
    public void GeneratePayload_TraVeChuoiEmvCoChuan_VoiCrc16()
    {
        // Arrange
        string bankCode = "MB";
        string stk = "0387431269";
        decimal amount = 250000m;
        string message = "DH12345";

        // Act
        string payload = VietQrGenerator.GeneratePayload(bankCode, stk, amount, message);

        // Assert
        Assert.NotNull(payload);
        Assert.StartsWith("000201", payload); // Payload Format Indicator
        Assert.Contains("970422", payload);   // BIN MB Bank
        Assert.Contains("0387431269", payload);
        Assert.Contains("250000", payload);
        Assert.Contains("DH12345", payload);
        Assert.Equal(4, payload[^4..].Length); // 4 ký tự cuối là Checksum CRC16
    }

    [Fact]
    public void GenerateQrBase64_TaoDuocAnhDataUrlPng()
    {
        // Arrange
        string payload = VietQrGenerator.GeneratePayload("VCB", "119002953584", 50000m, "TEST");

        // Act
        string base64 = VietQrGenerator.GenerateQrBase64(payload);

        // Assert
        Assert.NotNull(base64);
        Assert.StartsWith("data:image/png;base64,", base64);
        Assert.True(base64.Length > 100);
    }

    [Fact]
    public void GetBankBin_NhanDienDungMaNganHangPhoBien()
    {
        Assert.Equal("970422", VietQrGenerator.GetBankBin("MB"));
        Assert.Equal("970436", VietQrGenerator.GetBankBin("VCB"));
        Assert.Equal("970415", VietQrGenerator.GetBankBin("VIETINBANK"));
        Assert.Equal("970418", VietQrGenerator.GetBankBin("BIDV"));
        Assert.Equal("970416", VietQrGenerator.GetBankBin("ACB"));
    }
}
