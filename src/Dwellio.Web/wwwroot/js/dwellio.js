// Small, defensive storage helpers used by PlannerState. Every call is wrapped in try/catch
// because storage can be blocked (private mode, strict privacy settings, full quota).
window.dwellioStore = {
    get(key, session) {
        try { return (session ? sessionStorage : localStorage).getItem(key); }
        catch { return null; }
    },
    set(key, value, session) {
        try { (session ? sessionStorage : localStorage).setItem(key, value); return true; }
        catch { return false; }
    }
};
