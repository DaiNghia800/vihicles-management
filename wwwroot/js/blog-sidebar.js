// Shared sidebar functionality for blog pages
let allBlogsForSidebar = [];
let categoriesForSidebar = [];

/**
 * Load sidebar data (blogs and categories)
 * @param {number|null} currentBlogId - ID of current blog to exclude from recent posts (optional)
 */
async function loadSidebarData(currentBlogId = null) {
    try {
        const [blogsRes, categoriesRes] = await Promise.all([
            fetch('/blogs/api/list'),
            fetch('/blogs/api/categories')
        ]);

        if (blogsRes.ok) {
            allBlogsForSidebar = await blogsRes.json();
            renderRecentPosts(currentBlogId);
        }

        if (categoriesRes.ok) {
            categoriesForSidebar = await categoriesRes.json();
            renderCategories();
        }
    } catch (error) {
        console.error('Error loading sidebar data:', error);
    }
}

/**
 * Render recent posts in sidebar
 * @param {number|null} currentBlogId - ID of current blog to exclude
 */
function renderRecentPosts(currentBlogId = null) {
    const container = document.getElementById('recentPostsContainer');

    if (!allBlogsForSidebar || allBlogsForSidebar.length === 0) {
        container.innerHTML = '<p class="text-muted text-center py-3">No posts available</p>';
        return;
    }

    // Filter out current blog if viewing detail page
    let recentBlogs = allBlogsForSidebar;
    if (currentBlogId) {
        recentBlogs = allBlogsForSidebar.filter(b => b.uid !== parseInt(currentBlogId));
    }

    // Get 3 most recent blogs
    recentBlogs = recentBlogs.slice(0, 3);

    if (recentBlogs.length === 0) {
        container.innerHTML = '<p class="text-muted text-center py-3">No other posts available</p>';
        return;
    }

    container.innerHTML = recentBlogs.map(b => `
        <div class="recent-post-item">
            <div class="recent-post-content">
                <h6>
                    <a href="/blogs/${b.uid}">${truncateText(b.title, 60)}</a>
                </h6>
                <div class="date">
                   <i class="fa-solid fa-clock" style="color: #a8a8a8;"></i> ${formatDate(b.createdAt)}
                </div>
            </div>
        </div>
    `).join('');
}

/**
 * Render categories in sidebar
 */
function renderCategories() {
    const container = document.getElementById('categoriesContainer');

    if (!categoriesForSidebar || categoriesForSidebar.length === 0) {
        container.innerHTML = '<li class="text-muted text-center py-3">No categories</li>';
        return;
    }

    container.innerHTML = categoriesForSidebar.map(cat => {
        const count = allBlogsForSidebar.filter(b => b.categoryUid === cat.uid).length;
        return `
            <li>
                <a href="/blogs?category=${cat.uid}">
                    <i class="fa-solid fa-chevron-right" style="color: #ffab00;"></i>
                    ${cat.name} (${count})
                </a>
            </li>`;
    }).join('');
}

/**
 * Truncate text with ellipsis
 * @param {string} text - Text to truncate
 * @param {number} maxLength - Maximum length
 * @returns {string} Truncated text
 */
function truncateText(text, maxLength) {
    if (!text) return '';
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text;
}

/**
 * Format date to readable string
 * @param {string} dateString - Date string to format
 * @returns {string} Formatted date
 */
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
        month: 'long',
        day: 'numeric',
        year: 'numeric'
    });
}

// Export for use in other scripts (if using modules)
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { loadSidebarData, renderRecentPosts, renderCategories };
}