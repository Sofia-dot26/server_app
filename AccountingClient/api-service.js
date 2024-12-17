//Êמםענמככונû

const AUTH_CONTROLLER = "auth";

const USERS_CONTROLLER = "users";

const MATERIALS_CONTROLLER = "materials";

const SUPPLIERS_CONTROLLER = "suppliers";

const SUPPLIES_CONTROLLER = "supplies";

const SPENDS_CONTROLLER = "spend";

const EQUIPMENT_CONTROLLER = "equipment";

const REPORTS_CONTROLLER = "reports";

const HEALTH_CONTROLLER = "health";

const SYSTEM_CONTROLLER = "system";

//Ìועמהû
const METHOD_LOGIN = "login";

const METHOD_LOGOUT = "logout";

const METHOD_STATE = "state";

const METHOD_ADD = "add";

const METHOD_UPDATE = "update";

const METHOD_DELETE = "delete";

const METHOD_LIST = "list";

const METHOD_GET = "get";

const METHOD_CONSUMPTION = "consumption";

const METHOD_AVERAGE_CONSUMPTION = "average_consumption";

const METHOD_RESTS = "remaining";

const METHOD_SUPPLIES = "supplies";

const METHOD_CHECK = "check";

const METHOD_GET_INTERFACE = "get-interface";

const FIELD_SESSION_ID = 'session_id';

class ApiService {
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
    }

    async apiCall(controller, method, params = {}) {
        const url = new URL(`${this.baseUrl}/api/v1/${controller}/${method}`);
        Object.entries(params).forEach(([key, value]) => url.searchParams.append(key, value));

        const headers = {};
        const session_id = localStorage.getItem(FIELD_SESSION_ID);
        if (session_id) headers['X-Session-ID'] = session_id;

        try {
            const response = await fetch(url, { headers });
            const contentType = response.headers.get('Content-Type');
            if (response.status === 401) {
                localStorage.removeItem(FIELD_SESSION_ID);
                view("LoginView");
                return;
            }
            if (contentType && contentType.includes('application/json')) {
                const data = await response.json();
                if (data.session_id) localStorage.setItem(FIELD_SESSION_ID, data.session_id);
                if (data.message) showNotification(data.message);
                return data;
            }
            return await response.text();
        } catch (err) {
            showNotification(`Îרטבךא חאןנמסא: ${err.message}`);
            throw err;
        }
    }

    //Ìועמהû auth
    login(login, password) {
        return this.apiCall(AUTH_CONTROLLER, METHOD_LOGIN, { login, password });
    }
    logout() {
        return this.apiCall(AUTH_CONTROLLER, METHOD_LOGOUT);
    }
    loginState() {
        return this.apiCall(AUTH_CONTROLLER, METHOD_STATE);
    }
    // Ìועמהû users
    addUser(login, password, role) {
        return this.apiCall(USERS_CONTROLLER, METHOD_ADD, { login, password, role });
    }
    updateUser(id, login, password, role) {
        return this.apiCall(USERS_CONTROLLER, METHOD_UPDATE, { id, login, password, role });
    }
    deleteUser(id) {
        return this.apiCall(USERS_CONTROLLER, METHOD_DELETE, { id });
    }
    listUsers() {
        return this.apiCall(USERS_CONTROLLER, METHOD_LIST);
    }
    // Ìועמהû materials
    addMaterial(name, unit) {
        return this.apiCall(MATERIALS_CONTROLLER, METHOD_ADD, { name, unit });
    }

    updateMaterial(id, name, unit) {
        return this.apiCall(MATERIALS_CONTROLLER, METHOD_UPDATE, { id, name, unit });
    }

    deleteMaterial(id) {
        return this.apiCall(MATERIALS_CONTROLLER, METHOD_DELETE, { id });
    }

    getMaterial(id) {
        return this.apiCall(MATERIALS_CONTROLLER, METHOD_GET, { id });
    }

    listMaterials() {
        return this.apiCall(MATERIALS_CONTROLLER, METHOD_LIST);
    }

    // Ìועמהû suppliers
    addSupplier(name, contactInfo) {
        return this.apiCall(SUPPLIERS_CONTROLLER, METHOD_ADD, { name, contactInfo });
    }

    updateSupplier(id, name, contactInfo) {
        return this.apiCall(SUPPLIERS_CONTROLLER, METHOD_UPDATE, { id, name, contactInfo });
    }

    deleteSupplier(id) {
        return this.apiCall(SUPPLIERS_CONTROLLER, METHOD_DELETE, { id });
    }

    listSuppliers() {
        return this.apiCall(SUPPLIERS_CONTROLLER, METHOD_LIST);
    }

    // Ìועמהû supplies
    addSupply(name, contactInfo) {
        return this.apiCall(SUPPLIES_CONTROLLER, METHOD_ADD, { name, contactInfo });
    }

    updateSupply(id, name, contactInfo) {
        return this.apiCall(SUPPLIES_CONTROLLER, METHOD_UPDATE, { id, name, contactInfo });
    }

    deleteSupply(id) {
        return this.apiCall(SUPPLIES_CONTROLLER, METHOD_DELETE, { id });
    }

    getSupply(id) {
        return this.apiCall(SUPPLIES_CONTROLLER, METHOD_GET, { id });
    }

    listSupplies() {
        return this.apiCall(SUPPLIES_CONTROLLER, METHOD_LIST);
    }


    // Ìועמהû spend
    addSpend(material_id, quantity, date) {
        return this.apiCall(SPENDS_CONTROLLER, METHOD_ADD, { material_id, quantity, date });
    }

    updateSpend(id, material_id, quantity, date) {
        return this.apiCall(SPENDS_CONTROLLER, METHOD_UPDATE, { id, material_id, quantity, date });
    }

    deleteSpend(id) {
        return this.apiCall(SPENDS_CONTROLLER, METHOD_DELETE, { id });
    }

    getSpend(id) {
        return this.apiCall(SPENDS_CONTROLLER, METHOD_GET, { id });
    }

    listSpends() {
        return this.apiCall(SPENDS_CONTROLLER, METHOD_LIST);
    }

    // Ìועמהû equipment
    addEquipment(name, description) {
        return this.apiCall(EQUIPMENT_CONTROLLER, METHOD_ADD, { name, description });
    }

    updateEquipment(id, name, description) {
        return this.apiCall(EQUIPMENT_CONTROLLER, METHOD_UPDATE, { id, name, description });
    }

    deleteEquipment(id) {
        return this.apiCall(EQUIPMENT_CONTROLLER, METHOD_DELETE, { id });
    }

    getEquipment(id) {
        return this.apiCall(EQUIPMENT_CONTROLLER, METHOD_GET, { id });
    }

    listEquipment() {
        return this.apiCall(EQUIPMENT_CONTROLLER, METHOD_LIST);
    }

    // Ìועמהû reports
    consumption(start, end) {
        return this.apiCall(REPORTS_CONTROLLER, METHOD_CONSUMPTION, { start, end });
    }

    averageConsumption(start, end) {
        return this.apiCall(REPORTS_CONTROLLER, METHOD_AVERAGE_CONSUMPTION, { start, end });
    }

    remaining() {
        return this.apiCall(REPORTS_CONTROLLER, METHOD_RESTS);
    }

    supplies() {
        return this.apiCall(REPORTS_CONTROLLER, METHOD_SUPPLIES);
    }

    // Ìועמהû health
    checkHealth() {
        return this.apiCall(HEALTH_CONTROLLER, METHOD_CHECK);
    }

    getSystemInterface() {
        return this.apiCall(SYSTEM_CONTROLLER, METHOD_GET_INTERFACE);
    }
}
