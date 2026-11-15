// sessionStorage keeps each browser tab's shortlist separate.
window.dwellioComparison = {
    read() {
        try {
            const ids = JSON.parse(sessionStorage.getItem("dwellio.compare.v1") || "[]");
            return Array.isArray(ids)
                ? [...new Set(ids.filter(id => Number.isSafeInteger(id) && id > 0 && id <= 2147483647))].slice(0, 3)
                : [];
        } catch { return []; }
    },
    write(ids) {
        try {
            sessionStorage.setItem("dwellio.compare.v1", JSON.stringify(ids));
            return true;
        } catch { return false; }
    }
};

// Sourced examples use a separate key from synthetic demo apartment IDs.
window.dwellioMarket = {
    read() {
        try {
            const urls = JSON.parse(sessionStorage.getItem("dwellio.market.v1") || "[]");
            return Array.isArray(urls) ? urls.filter(x => typeof x === "string").slice(0, 3) : [];
        } catch { return []; }
    },
    write(urls) {
        try { sessionStorage.setItem("dwellio.market.v1", JSON.stringify(urls)); return true; }
        catch { return false; }
    }
};
