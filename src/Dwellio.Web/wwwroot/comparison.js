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
