const notification = document.getElementById('notification');
const roles = {
    admin: 'Администратор',
    dir: 'Начальник подразделения',
    acc: 'Учётчик',
};
const roles_rp = {
    admin: 'администратора',
    dir: 'начальника подразделения',
    acc: 'учётчика',
};

// Показ уведомления
function showNotification(message) {
    notification.innerText = message.trim();
    if (notification.innerText !== "") {
        notification.style.display = 'block';
        notification.onclick = () => notification.style.display = 'none';
        setTimeout(() => notification.style.display = 'none', 5000);
    }
}

// Запрос к API
const api = new ApiService(window.location.origin);

function showAllowedViews(allowedViews) {
    document.getElementById('control-panel').innerHTML = `Панель управления ${roles_rp[user_role] || user_role}`;
    document.getElementById('greetings').innerHTML = `Здравствуйте, ${user.login}!`;

    let element = document.getElementById('dashboard-links'); // Контейнер для ссылок
    element.innerHTML = ''; // Очищаем
    for(let viewId of allowedViews) { // И генерируем разрешённые ссылки
        let link = document.createElement('div');
        link.attributes['data-id'] = viewId;
        link.innerHTML = `<button class="dashboard-button" onclick="view('${viewId}View')">${window.views[viewId].title_main}</button>`;
        element.appendChild(link);
    }

    // Открываем DashboardView
    view("DashboardView");
}
function login(login, password) {
    api.login(login, password)
        .then(response => {
            try {
                if (response.success) {
                    // Сохраняем данные пользователя и сессии
                    window.user = response.user;
                    window.session = response.session;
                    window.session_id = response.session_id;
                    window.user_role = response.user_role;
                    window.allowed_controllers = response.allowed_controllers;
                    window.allowed_views = response.allowed_views;

                    // Сохраняем session_id в localStorage
                    localStorage.setItem("session_id", response.session_id);

                    // Обновляем видимость ссылок на дашборде
                    showAllowedViews(response.allowed_views || []);
                } else {
                    // В случае ошибки авторизации отображаем сообщение
                    showNotification(response.message || "Ошибка авторизации");
                    view("LoginView");
                }
            } catch (err) {
                // Обработка ошибок
                showNotification(`Ошибка запроса: ${err.message}`);
                view("LoginView");
            }
        });
}
function buttonLoginClick() {
    const username = document.getElementById("lv-login").value.trim();
    const password = document.getElementById("lv-password").value.trim();

    if (!username || !password) {
        showNotification("Введите логин и пароль");
        return;
    }

    login(username, password);
}

function buttonLogoutClick() {
    api
        .logout()
        .then(response => {
            view('LoginView')
        });
}


// Авторизация на старте
async function initializeApp() {
    // Доступные рабочие области
    await doInitViews();

    // Привязка обработчика кнопки входа
    document.getElementById("lb-enter").addEventListener("click", buttonLoginClick);
    document.getElementById("logout-button").addEventListener("click", buttonLogoutClick);

    // Проверка состояния авторизации
    const session_id = localStorage.getItem('session_id');
    if (session_id) {
        api.loginState() // Отправляем на сервер запрос состояния
            .then(state => { // При получении открываем либо дашборд-меню, либо таки обратно панель логина
                if (state.valid) {
                    window.user = state.user;
                    window.session = state.session;
                    window.session_id = state.session_id;
                    window.user_role = state.user_role;
                    window.allowed_controllers = state.allowed_controllers;
                    window.allowed_views = state.allowed_views;

                    showAllowedViews(state.allowed_views || []);
                    return;
                }
                view('LoginView');
            });
    } else {
        view('LoginView');
    }
}
// Привязка стартовых скриптов
document.addEventListener('DOMContentLoaded', initializeApp);