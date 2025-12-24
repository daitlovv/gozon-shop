const API_URL = window.location.hostname === 'localhost'
    ? 'http://localhost:8080'
    : '/api';

let currentUserId = null;

function showToast(message, type = 'success')
{
    let backgroundColor = "#28a745";

    if (type === 'error')
    {
        backgroundColor = "#dc3545";
    }
    else if (type === 'info')
    {
        backgroundColor = "#17a2b8";
    }
    else if (type !== 'success')
    {
        backgroundColor = "#6c757d";
    }

    Toastify({
        text: message,
        duration: 3000,
        gravity: "top",
        position: "right",
        backgroundColor: backgroundColor,
        stopOnFocus: true
    }).showToast();
}

function showLoading(button)
{
    const originalText = button.innerHTML;
    button.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Обработка...';
    button.disabled = true;
    return originalText;
}

function hideLoading(button, originalText)
{
    button.innerHTML = originalText;
    button.disabled = false;
}

async function apiRequest(url, options = {})
{
    try
    {
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        if (currentUserId && !url.includes('/accounts/'))
        {
            headers['user_id'] = currentUserId;
        }

        const response = await fetch(`${API_URL}${url}`, {
            ...options,
            headers
        });

        if (!response.ok)
        {
            const error = await response.json().catch(() => ({}));
            throw new Error(error.message || `HTTP ${response.status}`);
        }

        return await response.json();
    }
    catch (error)
    {
        console.error('API Error:', error);
        showToast(`Ошибка: ${error.message}`, 'error');
        throw error;
    }
}

function setUserId()
{
    const userIdInput = document.getElementById('userId');
    const userId = userIdInput.value.trim();

    if (!userId)
    {
        showToast('Введите User ID', 'error');
        return;
    }

    localStorage.setItem('gozon_user_id', userId);
    currentUserId = userId;

    document.getElementById('step1').style.display = 'none';
    document.getElementById('main-interface').style.display = 'block';

    document.getElementById('user-info').innerHTML = `
        <i class="bi bi-person"></i> ${userId.substring(0, 8)}...
    `;

    refreshData();

    showToast(`Добро пожаловать, пользователь ${userId.substring(0, 8)}...`, 'success');
}

async function createAccount()
{
    const button = document.querySelector('button[onclick="createAccount()"]');
    const originalText = showLoading(button);

    try
    {
        await apiRequest('/accounts', {
            method: 'POST',
            body: '{}'
        });
        showToast('Счет успешно создан!', 'success');
        await refreshBalance();
    }
    catch (error)
    {
        if (error.message.includes('ACCOUNT_ALREADY_EXISTS'))
        {
            showToast('Счет уже существует', 'info');
        }
    }
    finally
    {
        hideLoading(button, originalText);
    }
}

async function topUpAccount()
{
    const amountInput = document.getElementById('topupAmount');
    const amount = parseFloat(amountInput.value);

    if (!amount || amount <= 0)
    {
        showToast('Введите корректную сумму', 'error');
        return;
    }

    const button = document.querySelector('button[onclick="topUpAccount()"]');
    const originalText = showLoading(button);

    try
    {
        await apiRequest(`/accounts/${currentUserId}/topup`, {
            method: 'POST',
            body: JSON.stringify({ amount })
        });
        showToast(`Счет пополнен на ${amount} ₽`, 'success');
        await refreshBalance();
        amountInput.value = '';
    }
    finally
    {
        hideLoading(button, originalText);
    }
}

async function createOrder()
{
    const amount = parseFloat(document.getElementById('orderAmount').value);
    const description = document.getElementById('orderDescription').value.trim();

    if (!amount || amount <= 0)
    {
        showToast('Введите корректную сумму заказа', 'error');
        return;
    }

    if (!description)
    {
        showToast('Введите описание заказа', 'error');
        return;
    }

    const button = document.querySelector('button[onclick="createOrder()"]');
    const originalText = showLoading(button);

    try
    {
        const result = await apiRequest('/orders', {
            method: 'POST',
            body: JSON.stringify({ amount, description })
        });

        showToast(`Заказ создан! ID: ${result.order_id.substring(0, 8)}...`, 'success');

        document.getElementById('orderAmount').value = '';
        document.getElementById('orderDescription').value = '';

        setTimeout(refreshOrders, 3000);
    }
    finally
    {
        hideLoading(button, originalText);
    }
}

async function refreshBalance()
{
    try
    {
        const data = await apiRequest(`/accounts/${currentUserId}/balance`);
        document.getElementById('balance').textContent = `${data.balance} ₽`;
    }
    catch (error)
    {
        document.getElementById('balance').textContent = '0 ₽';
    }
}

async function refreshOrders()
{
    try
    {
        const orders = await apiRequest('/orders');
        const tbody = document.getElementById('orders-list');

        if (!orders || orders.length === 0)
        {
            tbody.innerHTML = `
                <tr>
                    <td colspan="5" class="text-center text-muted py-4">
                        <i class="bi bi-cart-x"></i> Нет заказов
                    </td>
                </tr>
            `;
            return;
        }

        tbody.innerHTML = orders.map(order => {
            const date = new Date(order.createdAt);
            const formattedDate = date.toLocaleString('ru-RU');

            let statusClass = 'status-new';
            let statusText = 'New';

            if (order.status === 'Finished')
            {
                statusClass = 'status-finished';
                statusText = 'Оплачен';
            }
            else if (order.status === 'Cancelled')
            {
                statusClass = 'status-cancelled';
                statusText = 'Отменен';
            }
            else
            {
                statusText = 'Новый';
            }

            return `
                <tr>
                    <td><small>${order.id.substring(0, 8)}...</small></td>
                    <td><strong>${order.amount} ₽</strong></td>
                    <td>${order.description}</td>
                    <td><span class="${statusClass}">${statusText}</span></td>
                    <td><small>${formattedDate}</small></td>
                </tr>
            `;
        }).join('');
    }
    catch (error)
    {
        const tbody = document.getElementById('orders-list');
        tbody.innerHTML = `
            <tr>
                <td colspan="5" class="text-center text-danger py-4">
                    <i class="bi bi-exclamation-triangle"></i> Ошибка загрузки заказов
                </td>
            </tr>
        `;
    }
}

async function refreshData()
{
    await Promise.all([
        refreshBalance(),
        refreshOrders()
    ]);
}

document.addEventListener('DOMContentLoaded', function() {
    const savedUserId = localStorage.getItem('gozon_user_id');

    if (savedUserId)
    {
        document.getElementById('userId').value = savedUserId;
        setUserId();
    }

    setInterval(() => {
        if (currentUserId)
        {
            refreshData();
        }
    }, 10000);
});