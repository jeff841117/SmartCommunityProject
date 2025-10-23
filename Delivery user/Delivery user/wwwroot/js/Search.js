document.addEventListener("DOMContentLoaded", function () {
    const input = document.getElementById("searchInput");
    const clearBtn = document.getElementById("clearBtn");
    const searchBtn = document.getElementById("searchBtn");

    if (!input || !clearBtn) {
        console.error("找不到 searchInput 或 clearBtn");
        return;
    }

    // 頁面載入時若已有 value（例如回填），顯示清除按鈕
    if (input.value && input.value.trim()) {
        clearBtn.style.display = "block";
    } else {
        clearBtn.style.display = "none";
    }

    // 當輸入框有文字時顯示 / 隱藏清除按鈕
    input.addEventListener("input", function () {
        clearBtn.style.display = this.value.trim() ? "block" : "none";
    });

    // 點擊清除按鈕：清空文字並保持焦點
    clearBtn.addEventListener("click", function (e) {
        e.preventDefault();
        input.value = "";
        clearBtn.style.display = "none";
        input.focus();

        // 可觸發 input 事件（若有其他 listener 依賴）
        const evt = new Event('input', { bubbles: true });
        input.dispatchEvent(evt);
    });

    // 若按查詢你需要執行的行為（可自行替換）
    if (searchBtn) {
        searchBtn.addEventListener("click", function () {
            const q = input.value.trim();
            // 範例：顯示結果容器（你可替換為真正的查詢）
            const resultContainer = document.getElementById("resultContainer");
            const title = document.getElementById("resultTitle");
            const content = document.getElementById("resultContent");
            if (!q) {
                if (resultContainer) resultContainer.style.display = "none";
                return;
            }
            if (resultContainer && title && content) {
                title.textContent = `搜尋結果：${q}`;
                content.textContent = `（這裡顯示查詢回傳內容）`;
                resultContainer.style.display = "block";
            }
        });
    }
});
