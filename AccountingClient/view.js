const breadcrumbs = [];

/**
 * Инициализация рабочих панелей.
 * @returns {Promise<void>}
 */
async function doInitViews() {
    window.views = await api.getSystemInterface();
    // Шаблон вывода панелей
    const template = `
<div class="view" id="{{name}}View">
    <div id="{{name}}Table"></div>
    <button class="back-button" onclick="goBack()">Назад</button>
</div>
<!-- Добавление или редактирование {{name}} -->
<div class="view" id="Add{{name}}View">
    <div id="{{name}}Form"></div>
    <button class="back-button" onclick="goBack()">Назад</button>
</div>

`;
    initViews(views, template, "views-container");
}

/**
 * код панелей согласно данным и шаблону, и устанавливает их в переданный контейнер
 * @param views
 * @param template
 * @param container_id
 */
function initViews(views, template, container_id) {
    const container = document.getElementById(container_id);
    console.log('Заполняем панели', container);
    if (!container) {
        console.error(`Контейнер с id "${container_id}" не найден.`);
        return;
    }

    // Очищаем контейнер
    container.innerHTML = '';

    for (const [key, value] of Object.entries(views)) {
        const viewHtml = template
            .replace(/{{name}}/g, key)
            .trim(); // Заменяем {{name}} на имя представления

        const viewElement = document.createElement('div');
        viewElement.innerHTML = `
<!-- ${value.description} -->
${viewHtml}
`;
        console.log('Добавляем', viewElement);
        container.appendChild(viewElement);
    }
}

function getFormData(viewShortName) {
    const currentView = window.views[viewShortName];
    const container = document.querySelector(`#${viewShortName}Form`);
    // Собираем данные из формы
    const formData = {};
    for (const [fieldName, fieldParams] of Object.entries(currentView.add)) {
        const input = container.querySelector(`[name="${fieldName}"]`);
        if (!input) continue;

        if (fieldParams.type === "radio-images" || fieldParams.type === "radio") {
            const checkedInput = container.querySelector(`[name="${fieldName}"]:checked`);
            formData[fieldName] = checkedInput ? checkedInput.value : null;
        } else if (fieldParams.type === "selectbox") { // Селектбокс из предустановленного небольшого числа значений
            formData[fieldName] = input.value || null;
        } else { // Все остальные типы инпутов, включая hidden у выбора из справочника
            formData[fieldName] = input.value || '';
        }
    }
    return formData;
}

/**
 * Выполняет сбор и отправку формы на сервер.
 * Поскольку формы у нас не стандартные, а скриптовые и под ajax, без этого никуда.
 * @param viewShortName
 * @param id
 * @returns {Promise<void>}
 */
async function submitForm(viewShortName, id = null) {
    const currentView = window.views[viewShortName];
    const container = document.querySelector(`#${viewShortName}Form`);

    if (!container) {
        console.error(`Форма для ${viewShortName} не найдена`);
        return;
    }

    const formData = getFormData(viewShortName);

    // Определяем метод
    const method = id === null ? METHOD_ADD : METHOD_UPDATE;
// Блокируем кнопку во время выполнения
    const submitButton = container.querySelector('button[type="button"]');
    if (submitButton) submitButton.disabled = true;
    try {


        // Выполняем API-запрос
        const result = await api.apiCall(currentView.controller, method, {...formData, id});

        if (result?.success) {
            // Возвращаемся назад при успехе
            goBack();
        } else {
            // alert(result?.message || "Произошла ошибка");
        }
    } catch (error) {
        console.error(`Ошибка при отправке формы для ${viewShortName}:`, error);
        alert("Не удалось выполнить запрос. Попробуйте снова.");
    } finally {
        // Разблокируем кнопку
        if (submitButton) submitButton.disabled = false;
    }
}

/**
 * Выбранный элемент будет отображён на форме выбора
 * @param controller
 * @param fieldName
 * @param currentId
 * @param currentTitle
 */
function selectFromDictionary(controller, fieldName, currentId, currentTitle) {
    window.dictionarySelectMode = fieldName;
    if (!window.selectedValues) {
        window.selectedValues = {};
    }
    const formData = getFormData(controller);
    window.selectedValues[fieldName] = [currentId || null, currentTitle || ''];
    for(let field in formData) {
        if (!window.selectedValues.hasOwnProperty(field) && formData[field]) {
            window.selectedValues[field] = [formData[field], formData[field]];
        }
    }
    view(`${controller}View`);
}

function doSelectFromDictionary(id, name) {
    if (window.dictionarySelectMode) {
        window.selectedValues[window.dictionarySelectMode] = [id, name];
        breadcrumbs.pop();
        makeForm(window.lastController, window.objectId, window.objectObject);
    }
}

/**
 * Создаёт форму из информации, возвращённой с сервера
 * @param viewShortName
 * @param id
 * @param object
 * @returns {Promise<void>}
 */
async function makeForm(viewShortName, id = null, object = null) {
    const currentView = window.views[viewShortName];
    const container = `#${viewShortName}Form`;

    const title = id === null ? `Создать ${currentView.title}` : `Редактировать ${currentView.title} № ${id}`;
    document.querySelector(container).innerHTML = `<h2>${title}</h2>`;

    // Создаём форму
    let formHtml = '';
    for (const [fieldName, fieldParams] of Object.entries(currentView.add)) {
        let selectedValues = window.selectedValues ? window.selectedValues[fieldName] || [] : [];
        let fieldDescription = (fieldName.endsWith('_id') && object)
            ? object[fieldName.replace(/_id$/, '_name')] || object[fieldName]
            : null;
        let selectedId = selectedValues[0] || (object && object[fieldName]),
            selectedName = selectedValues[1] || fieldDescription || "Выбрать";
        let inputHtml = '';
        if (fieldParams.type === "radio-images") {
            inputHtml = Object.entries(fieldParams.values).map(([fieldValue, label]) => {
                const checked = object && object[fieldName] === fieldValue ? 'checked=1' : '';
                return `<label class="label-radio">
<div class="label-radio">${label}</div>
<img src="img/icon-${viewShortName}-${fieldName}-${fieldValue}.png" alt="${label}" class="icon-radio" />
<input type="radio" name="${fieldName}" value="${fieldValue || null}" ${checked} onchange="radioOnChange('${fieldName}',this.value)"/>
</label>`;
            }).join('');
        } else if (fieldParams.type === "selectbox" || fieldParams.type === "radio") {
            inputHtml = `<select id="input-${fieldName}" name="${fieldName}">${Object.entries(fieldParams.values).map(([key, label]) => `<option value="${key || null}">${label}</option>`).join('')}</select>`;
        } else if (fieldParams.type === "dictionary") { // Выбор из справочника
            window.objectId = id;
            window.objectObject = object;
            window.lastController = viewShortName;
            inputHtml = `<button class="button-chose-dict${selectedId ? " selected" : ""}" onclick="selectFromDictionary('${fieldParams.controller}', '${fieldName}', '${object ? object[fieldName] || "" : ""}')">${selectedName}</button>
<input type="hidden" id="input-${fieldName}" name="${fieldName}" value="${selectedId}" />`;
        } else {
            inputHtml = `<input type="${fieldParams.type}" id="input-${fieldName}" name="${fieldName}" value="${(object ? (object[fieldName] || '') : '') || selectedId || fieldParams['default_value'] || ''}" />`;
        }
        formHtml += `<div id="div-${fieldName}"><label id="label-${fieldName}" for="input-${fieldName}">${fieldParams.text}</label>${inputHtml}</div>`;
    }

    formHtml += `<button type="button" class="button-add" onclick="submitForm('${viewShortName}', ${id})">${id === null ? 'Добавить' : 'Сохранить'}</button>`;

    document.querySelector(container).innerHTML += formHtml;

    // Вызываем функцию для соответствующего view
    view(`Add${viewShortName}View`);
}

/**
 * Отображает запрошенную функциональную панель и скрывает остальные
 * @param name
 * @param params
 */
function view(name, params = {}) {
    console.log('Открываем', name, params);
    document.querySelectorAll('.view').forEach(v => v.classList.remove('active'));
    const targetView = document.getElementById(name);
    if (targetView) {
        targetView.classList.add('active');
        // Обновляем хлебные крошки
        if (breadcrumbs.length === 0 || breadcrumbs[breadcrumbs.length - 1].name !== name) {
            breadcrumbs.push({name, params});
        }
        updateBreadcrumbs();
        onOpenView(name, params);
    }
}


function updateBreadcrumbs() {
    const container = document.getElementById('breadcrumbs');
    if (!container) return;

    container.innerHTML = ''; // Очищаем старые крошки
    breadcrumbs.forEach((crumb, index) => {
        const crumbEl = document.createElement('span');
        crumbEl.innerText = crumb.name.replace("View", "");
        crumbEl.classList.add('breadcrumb');
        crumbEl.onclick = () => {
            breadcrumbs.splice(index + 1); // Удаляем все крошки после выбранной
            view(crumb.name, crumb.params);
        };
        container.appendChild(crumbEl);

        if (index < breadcrumbs.length - 1) {
            const separator = document.createElement('span');
            separator.innerText = ' > ';
            container.appendChild(separator);
        }
    });
}

/**
 * Переход из текущей панели на предыдущую в порядке открытия
 */
function goBack() {
    if (breadcrumbs.length > 1) {
        breadcrumbs.pop(); // Удаляем последний элемент
        const lastCrumb = breadcrumbs[breadcrumbs.length - 1];
        view(lastCrumb.name, lastCrumb.params);
    }
    window.dictionarySelectMode = false; // Переход назад сбрасывает режим выбора
}

/**
 * Создаёт таблицу из переданных данных.
 * Таблица поддерживает поиск с подсветкой, сортировку по колонкам, добавление элементов, редактирование, удаление, выбор
 * @param data
 * @param containerId
 * @param headers
 * @param actions
 * @param viewShortName
 */
function createTable(data, containerId, headers, actions, viewShortName) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.error(`Контейнер с id "${containerId}" не найден.`);
        return;
    }

    // Очищаем контейнер
    container.innerHTML = '';

    // Создаём поле для добавления
    if (actions.add && !window.dictionarySelectMode) {
        const addButton = document.createElement('button');
        addButton.innerText = actions.add_text || 'Добавить';
        addButton.className = 'button-add';
        addButton.onclick = () => makeForm(viewShortName, null, null);
        container.appendChild(addButton);
    }

    // Состояние сортировки
    let sortState = {field: null, direction: null};

    // Создаём поле поиска
    const searchBox = document.createElement('input');
    searchBox.type = 'text';
    searchBox.placeholder = 'Поиск...';
    searchBox.oninput = () => filterAndHighlightRows();
    container.appendChild(searchBox);

    // Создаём таблицу
    const table = document.createElement('table');
    table.classList.add('data-table');

    // Создаём заголовок таблицы
    const tableHeader = document.createElement('thead');
    const headerRow = document.createElement('tr');
    for (const [field, label] of Object.entries(headers)) {
        const th = document.createElement('th');
        th.innerHTML = `${label} <span class="sort-indicator">↕️</span>`;
        th.style.cursor = 'pointer';
        th.dataset.field = field;
        th.onclick = () => toggleSort(field);
        headerRow.appendChild(th);
    }
    if (actions.viewMode || actions.edit || actions.delete || window.dictionarySelectMode) {
        const th = document.createElement('th');
        th.innerText = 'Действия';
        headerRow.appendChild(th);
    }
    tableHeader.appendChild(headerRow);
    table.appendChild(tableHeader);

    // Создаём тело таблицы
    const tableBody = document.createElement('tbody');
    renderRows(data);
    table.appendChild(tableBody);
    container.appendChild(table);

    // Функция рендеринга строк
    function renderRows(data) {
        tableBody.innerHTML = '';

        // Сортировка данных
        const sortedData = [...data];
        if (sortState.field && sortState.direction) {
            sortedData.sort((a, b) => {
                const valueA = a[sortState.field] || '';
                const valueB = b[sortState.field] || '';

                const isNumericA = !isNaN(parseFloat(valueA)) && isFinite(valueA);
                const isNumericB = !isNaN(parseFloat(valueB)) && isFinite(valueB);

                if (isNumericA && isNumericB) {
                    return sortState.direction === 'asc'
                        ? parseFloat(valueA) - parseFloat(valueB)
                        : parseFloat(valueB) - parseFloat(valueA);
                }

                return sortState.direction === 'asc'
                    ? String(valueA).localeCompare(String(valueB))
                    : String(valueB).localeCompare(String(valueA));
            });
        }

        // Рендер строк
        sortedData.forEach(item => {
            const row = document.createElement('tr');

            for (const field in headers) {
                const td = document.createElement('td');
                td.innerText = item[field] || '';
                td.setAttribute('data-original', item[field] || ''); // Сохраняем оригинальный текст
                row.appendChild(td);
            }

            // Действия
            if (actions.viewMode || actions.edit || actions.delete || window.dictionarySelectMode) {
                const actionsCell = document.createElement('td');

                if (window.dictionarySelectMode) {
                    const selectButton = document.createElement('button');
                    selectButton.className = 'table-button-wide';
                    selectButton.innerHTML =
                        item.id === window.selectedValues[window.dictionarySelectMode][0]
                            ? '✅ Выбрано'
                            : 'Выбрать';
                    selectButton.title = 'Выбрать';
                    selectButton.onclick = () =>
                        doSelectFromDictionary(item.id, item.name);
                    actionsCell.appendChild(selectButton);
                } else {
                    if (actions.viewMode === 'table') {
                        const viewButton = document.createElement('button');
                        viewButton.className = 'table-button-wide';
                        viewButton.innerText = 'Смотреть';
                        viewButton.title = 'Смотреть';
                        viewButton.onclick = () => {
                            document.getElementById('TableViewHeader').innerHTML = item?.data?.legend;
                            createTable(item?.data?.values, 'TableViewTable', item?.data?.headers, {}, 'Table');
                            view('TableView');
                        }
                        actionsCell.appendChild(viewButton);
                    }

                    if (actions.edit) {
                        const editButton = document.createElement('div');
                        editButton.className = 'table-button';
                        editButton.innerText = '📝';
                        editButton.title = 'Редактировать';
                        editButton.onclick = () =>
                            makeForm(viewShortName, item.id, item);
                        actionsCell.appendChild(editButton);
                    }

                    if (actions.delete) {
                        const deleteButton = document.createElement('div');
                        deleteButton.className = 'table-button';
                        deleteButton.innerText = '❌';
                        deleteButton.title = 'Удалить';
                        deleteButton.onclick = () => {
                            if (confirm('Вы уверены, что хотите удалить этот элемент?')) {
                                api.apiCall(
                                    window.views[viewShortName].controller,
                                    METHOD_DELETE,
                                    {id: item.id}
                                ).then(response => {
                                    if (response.success) {
                                        view(viewShortName + 'View');
                                    }
                                });
                            }
                        };
                        actionsCell.appendChild(deleteButton);
                    }
                }
                row.appendChild(actionsCell);
            }

            tableBody.appendChild(row);
        });

        filterAndHighlightRows();
    }

    // Фильтрация и подсветка строк
    function filterAndHighlightRows() {
        const filter = searchBox.value.trim().toLowerCase();
        if (!filter) {
            Array.from(tableBody.rows).forEach(row => {
                row.style.display = '';
                Array.from(row.cells).forEach(cell => {
                    const original = cell.getAttribute('data-original');
                    if (original) cell.innerHTML = original; // Восстанавливаем оригинальный текст
                });
            });
            return;
        }

        Array.from(tableBody.rows).forEach(row => {
            let matches = false;
            Array.from(row.cells).forEach((cell, index) => {
                if (index === row.cells.length - 1) return; // Пропускаем колонку "Действия"

                const original = cell.getAttribute('data-original');
                const cellText = original ? original.toLowerCase() : '';
                if (cellText.includes(filter)) {
                    matches = true;

                    // Подсветка совпадений
                    const regex = new RegExp(`(${filter})`, 'gi');
                    cell.innerHTML = original.replace(regex, match => {
                        return `<span class="highlight">${match}</span>`;
                    });
                } else if (original) {
                    cell.innerHTML = original; // Убираем предыдущие подсветки
                }
            });

            row.style.display = matches ? '' : 'none';
        });
    }

    // Смена сортировки
    function toggleSort(field) {
        if (sortState.field === field) {
            sortState.direction =
                sortState.direction === 'asc'
                    ? 'desc'
                    : sortState.direction === 'desc'
                        ? null
                        : 'asc';
        } else {
            sortState.field = field;
            sortState.direction = 'asc';
        }

        // Обновляем индикаторы сортировки
        Array.from(headerRow.children).forEach(th => {
            const thField = th.dataset.field;
            const indicator = th.querySelector('.sort-indicator');
            if (indicator) {
                indicator.innerText = thField === field
                    ? sortState.direction === 'asc'
                        ? '▲'
                        : '▼'
                    : '↕️';
            }
        });

        renderRows(data);
    }
}

/**
 * Выполняет автоматические действия при открытии панели.
 * @param viewName
 * @param params
 * @returns {Promise<void>}
 */
async function onOpenView(viewName, params) {
    const viewShortName = viewName.replace("View", ""); // Убираем суффикс "View"
    if (window.views[viewShortName]) {
        const {controller, header, title} = window.views[viewShortName];

        try {
            const data = await api.apiCall(controller, "list"); // Вызываем метод API list
            let actions = {
                add_text: `Добавить ${title}`,
            };

            if (window.views[viewShortName].viewMode) {
                actions.viewMode = window.views[viewShortName].viewMode;
            }

            if (!window.views[viewShortName].nocreate) {
                actions.add = true;
            }
            if (!window.views[viewShortName].noedit) {
                actions.edit = true;
            }
            if (!window.views[viewShortName].nodelete) {
                actions.delete = true;
            }

            createTable(
                data,
                `${viewShortName}Table`,
                header,
                actions,
                viewShortName
            );
        } catch (err) {
            console.error(`Ошибка загрузки данных для ${viewShortName}:`, err.message);
            showNotification(`Ошибка загрузки данных: ${err.message}`);
        }
    } else {
        // console.warn(`onOpenView: Неизвестное представление "${viewName}"`);
    }
}

function radioOnChange(name, value) {
    if (name === 'report_type') {
        let period_start = document.getElementById('div-period_start'),
            period_end = document.getElementById('div-period_end');
        if (value === 'supplies' || value === 'remaining_materials') {
            period_start.style.display = 'none';
            period_end.style.display = 'none';
        } else {
            period_start.style.display = '';
            period_end.style.display = '';
        }
    }
}