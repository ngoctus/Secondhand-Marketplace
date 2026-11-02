// Realtime giá đấu giá. Dùng ở trang chi tiết sản phẩm đấu giá.
// Cách dùng: nhúng signalr.min.js (CDN) rồi nhúng file này, trang có <div id="auction" data-id="@auctionId">
// và thẻ <span id="current-price">.
const auctionEl = document.getElementById("auction");

if (auctionEl) {
    const auctionId = Number(auctionEl.dataset.id);
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/auction")
        .withAutomaticReconnect()
        .build();

    connection.on("PriceUpdated", (id, price) => {
        if (id === auctionId)
            document.getElementById("current-price").textContent = price.toLocaleString("vi-VN") + "đ";
    });

    connection.start().then(() => connection.invoke("JoinAuction", auctionId));
}
