// ============================================================
//  profileStorage.js  →  wwwroot/js/profileStorage.js
// ============================================================
window.profileStorage = {

    saveAvatar: function(userId, dataUrl) {
        try {
            localStorage.setItem('pf_avatar_' + userId, dataUrl);
            window.dispatchEvent(new CustomEvent('fs_avatarChanged', {
                detail: { userId: userId, dataUrl: dataUrl }
            }));
        } catch (e) { console.warn('[profileStorage] saveAvatar:', e); }
    },

    getAvatar: function(userId) {
        try { return localStorage.getItem('pf_avatar_' + userId) || null; }
        catch (e) { return null; }
    },

    saveCover: function(userId, dataUrl) {
        try { localStorage.setItem('pf_cover_' + userId, dataUrl); }
        catch (e) { console.warn('[profileStorage] saveCover:', e); }
    },

    getCover: function(userId) {
        try { return localStorage.getItem('pf_cover_' + userId) || null; }
        catch (e) { return null; }
    },

    onAvatarChanged: function(dotNetRef, userId) {
        try {
            var handler = function(e) {
                if (e.detail && e.detail.userId === userId) {
                    dotNetRef.invokeMethodAsync('OnAvatarChangedFromJs', e.detail.dataUrl)
                        .catch(function(err) { console.warn('[profileStorage] invoke:', err); });
                }
            };
            window['_avatarHandler_' + userId] = handler;
            window.addEventListener('fs_avatarChanged', handler);
        } catch (e) { console.warn('[profileStorage] onAvatarChanged:', e); }
    },

    offAvatarChanged: function(userId) {
        try {
            var handler = window['_avatarHandler_' + userId];
            if (handler) {
                window.removeEventListener('fs_avatarChanged', handler);
                delete window['_avatarHandler_' + userId];
            }
        } catch (e) { }
    }
};