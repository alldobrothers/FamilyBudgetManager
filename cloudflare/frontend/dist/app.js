// Family Budget Manager - Cloudflare D1 Edition Client
const API_BASE = '/api';
const DEFAULT_FAMILY_ID = 'fam-sharma';

let state = {
  currentTab: 'dashboard',
  familyId: DEFAULT_FAMILY_ID,
  categories: [],
  charts: {}
};

// Initialize Application
document.addEventListener('DOMContentLoaded', async () => {
  await loadCategories();
  await loadDashboard();
});

// Tab Switching
function switchTab(tabId) {
  state.currentTab = tabId;
  document.querySelectorAll('.tab-panel').forEach(p => p.style.display = 'none');
  const activePanel = document.getElementById(`tab-${tabId}`);
  if (activePanel) activePanel.style.display = 'block';

  document.querySelectorAll('.nav-item').forEach(item => {
    item.classList.remove('active');
    if (item.getAttribute('onclick')?.includes(tabId)) {
      item.classList.add('active');
    }
  });

  // Load relevant data
  if (tabId === 'dashboard') loadDashboard();
  if (tabId === 'expenses') loadExpenses();
  if (tabId === 'incomes') loadIncomes();
  if (tabId === 'bills') loadBills();
  if (tabId === 'debts') loadDebts();
  if (tabId === 'obligations') loadObligations();
  if (tabId === 'reports') loadReports();
}

// 1. Load Categories
async function loadCategories() {
  try {
    const res = await fetch(`${API_BASE}/categories`);
    if (res.ok) {
      state.categories = await res.json();
      populateCategorySelects();
    }
  } catch (err) {
    console.error('Failed to load categories', err);
  }
}

function populateCategorySelects() {
  const expSelect = document.getElementById('expCategory');
  const incSelect = document.getElementById('incCategory');
  if (expSelect) {
    const expenseCats = state.categories.filter(c => c.Type === 1);
    expSelect.innerHTML = expenseCats.map(c => `<option value="${c.Id}">${c.Name}</option>`).join('');
  }
  if (incSelect) {
    const incomeCats = state.categories.filter(c => c.Type === 2);
    incSelect.innerHTML = incomeCats.map(c => `<option value="${c.Id}">${c.Name}</option>`).join('');
  }
}

// 2. Load Dashboard
async function loadDashboard() {
  try {
    const res = await fetch(`${API_BASE}/dashboard/summary`, {
      headers: { 'X-Family-Id': state.familyId }
    });
    if (!res.ok) throw new Error('Dashboard API error');
    const data = await res.json();

    // Update KPI Cards
    document.getElementById('kpiIncome').textContent = `₹${data.metrics.totalIncome.toLocaleString('en-IN')}`;
    document.getElementById('kpiExpense').textContent = `₹${data.metrics.totalExpenses.toLocaleString('en-IN')}`;
    document.getElementById('kpiSavings').textContent = `₹${data.metrics.netSavings.toLocaleString('en-IN')}`;
    document.getElementById('kpiDebt').textContent = `₹${data.metrics.totalDebt.toLocaleString('en-IN')}`;

    if (data.family) {
      document.getElementById('householdName').textContent = data.family.Name;
    }

    // Render Recent Transactions
    const txBody = document.getElementById('recentTransactionsTable');
    if (data.recentExpenses && data.recentExpenses.length > 0) {
      txBody.innerHTML = data.recentExpenses.map(t => `
        <tr>
          <td><strong style="color: #fff;">${t.Title}</strong></td>
          <td><span class="badge-tag" style="background: rgba(255,255,255,0.08); border-left: 3px solid ${t.ColorHex || '#6366f1'};">${t.CategoryName || 'General'}</span></td>
          <td style="color: #ef4444; font-weight: 600;">₹${Number(t.Amount).toLocaleString('en-IN')}</td>
          <td>${t.PaidByName || 'Member'}</td>
          <td>${t.PaymentMethod || 'UPI'}</td>
          <td style="color: #94a3b8;">${new Date(t.ExpenseDate).toLocaleDateString()}</td>
        </tr>
      `).join('');
    } else {
      txBody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: #64748b;">No recent transactions recorded.</td></tr>';
    }

    // Render Upcoming Bills Widget
    const billsContainer = document.getElementById('upcomingBillsList');
    if (data.upcomingBills && data.upcomingBills.length > 0) {
      billsContainer.innerHTML = data.upcomingBills.map(b => `
        <div style="display: flex; justify-content: space-between; align-items: center; padding: 12px; margin-bottom: 8px; background: rgba(255,255,255,0.03); border-radius: 8px;">
          <div>
            <div style="font-weight: 600;">${b.BillName}</div>
            <small style="color: #94a3b8;">Due: ${new Date(b.DueDate).toLocaleDateString()}</small>
          </div>
          <div style="font-weight: 700; color: #f59e0b;">₹${Number(b.ExpectedAmount).toLocaleString('en-IN')}</div>
        </div>
      `).join('');
    } else {
      billsContainer.innerHTML = '<p style="color: #64748b; font-size: 0.9rem;">No upcoming bills pending.</p>';
    }

    // Render Category Chart
    renderCategoryChart(data.categoryBreakdown || []);

  } catch (err) {
    console.error('Failed to load dashboard', err);
  }
}

function renderCategoryChart(items) {
  const ctx = document.getElementById('categoryChart')?.getContext('2d');
  if (!ctx) return;

  if (state.charts.category) {
    state.charts.category.destroy();
  }

  const labels = items.map(i => i.Name);
  const data = items.map(i => i.TotalAmount);
  const colors = items.map(i => i.ColorHex || '#6366f1');

  state.charts.category = new Chart(ctx, {
    type: 'doughnut',
    data: {
      labels: labels.length ? labels : ['No Expenses'],
      datasets: [{
        data: data.length ? data : [1],
        backgroundColor: colors.length ? colors : ['#334155'],
        borderWidth: 0
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { position: 'bottom', labels: { color: '#94a3b8', boxWidth: 12 } }
      }
    }
  });
}

// 3. Load Expenses
async function loadExpenses() {
  try {
    const res = await fetch(`${API_BASE}/expenses`, {
      headers: { 'X-Family-Id': state.familyId }
    });
    const items = await res.json();
    const tbody = document.getElementById('expensesTableBody');
    if (items.length > 0) {
      tbody.innerHTML = items.map(e => `
        <tr>
          <td><strong style="color: #fff;">${e.Title}</strong><br><small style="color: #64748b;">${e.MerchantOrShop || ''}</small></td>
          <td><span class="badge-tag" style="background: rgba(255,255,255,0.08); border-left: 3px solid ${e.ColorHex || '#6366f1'};">${e.CategoryName || 'General'}</span></td>
          <td style="color: #ef4444; font-weight: 600;">₹${Number(e.Amount).toLocaleString('en-IN')}</td>
          <td>${e.PaidByName || 'Member'}</td>
          <td>${e.PaymentMethod || 'UPI'}</td>
          <td style="color: #94a3b8;">${new Date(e.ExpenseDate).toLocaleDateString()}</td>
          <td><button class="btn-primary-gradient" style="padding: 4px 10px; font-size: 0.75rem; background: #ef4444;" onclick="deleteExpense('${e.Id}')"><i class="bi bi-trash"></i></button></td>
        </tr>
      `).join('');
    } else {
      tbody.innerHTML = '<tr><td colspan="7" style="text-align: center; color: #64748b;">No expenses recorded yet.</td></tr>';
    }
  } catch (err) {
    console.error('Failed to load expenses', err);
  }
}

// 4. Load Incomes
async function loadIncomes() {
  try {
    const res = await fetch(`${API_BASE}/incomes`, {
      headers: { 'X-Family-Id': state.familyId }
    });
    const items = await res.json();
    const tbody = document.getElementById('incomesTableBody');
    if (items.length > 0) {
      tbody.innerHTML = items.map(i => `
        <tr>
          <td><strong style="color: #fff;">${i.Title}</strong><br><small style="color: #64748b;">${i.IncomeSource || ''}</small></td>
          <td><span class="badge-tag" style="background: rgba(16, 185, 129, 0.15); color: #10b981;">Income</span></td>
          <td style="color: #10b981; font-weight: 600;">₹${Number(i.Amount).toLocaleString('en-IN')}</td>
          <td>${i.RecipientName || 'Member'}</td>
          <td>${i.PaymentMethod || 'Bank'}</td>
          <td style="color: #94a3b8;">${new Date(i.DateReceived).toLocaleDateString()}</td>
        </tr>
      `).join('');
    } else {
      tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: #64748b;">No income transactions recorded yet.</td></tr>';
    }
  } catch (err) {
    console.error('Failed to load incomes', err);
  }
}

// 5. Load Bills
async function loadBills() {
  try {
    const res = await fetch(`${API_BASE}/bills`, {
      headers: { 'X-Family-Id': state.familyId }
    });
    const data = await res.json();
    const tbody = document.getElementById('billsTableBody');
    if (data.occurrences && data.occurrences.length > 0) {
      tbody.innerHTML = data.occurrences.map(o => `
        <tr>
          <td><strong style="color: #fff;">${o.BillName}</strong></td>
          <td>${o.Payee || 'Utility'}</td>
          <td style="color: #f59e0b; font-weight: 600;">₹${Number(o.ExpectedAmount).toLocaleString('en-IN')}</td>
          <td style="color: #94a3b8;">${new Date(o.DueDate).toLocaleDateString()}</td>
          <td><span class="badge-tag" style="background: ${o.Status === 2 ? 'rgba(16,185,129,0.2)' : 'rgba(245,158,11,0.2)'}; color: ${o.Status === 2 ? '#10b981' : '#f59e0b'};">${o.Status === 2 ? 'Paid' : 'Pending'}</span></td>
          <td>
            ${o.Status === 1 ? `<button class="btn-primary-gradient" style="padding: 5px 12px; font-size: 0.8rem; background: #10b981;" onclick="markBillPaid('${o.Id}', ${o.ExpectedAmount})">Mark Paid</button>` : '<span style="color:#64748b;">Completed</span>'}
          </td>
        </tr>
      `).join('');
    } else {
      tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: #64748b;">No recurring bills configured.</td></tr>';
    }
  } catch (err) {
    console.error('Failed to load bills', err);
  }
}

// 6. Load Debts
async function loadDebts() {
  try {
    const res = await fetch(`${API_BASE}/debts`, {
      headers: { 'X-Family-Id': state.familyId }
    });
    const data = await res.json();
    const tbody = document.getElementById('debtsTableBody');
    if (data.debts && data.debts.length > 0) {
      tbody.innerHTML = data.debts.map(d => `
        <tr>
          <td><strong style="color: #fff;">${d.CreditorOrDebtorName}</strong></td>
          <td><span class="badge-tag" style="background: ${d.DebtType === 1 ? 'rgba(239,68,68,0.2)' : 'rgba(16,185,129,0.2)'}; color: ${d.DebtType === 1 ? '#ef4444' : '#10b981'};">${d.DebtType === 1 ? 'Borrowed' : 'Lent'}</span></td>
          <td>₹${Number(d.OriginalPrincipal).toLocaleString('en-IN')}</td>
          <td style="font-weight: 600; color: #f59e0b;">₹${Number(d.CurrentBalance).toLocaleString('en-IN')}</td>
          <td><span class="badge-tag" style="background: rgba(255,255,255,0.08);">${d.Status === 3 ? 'Paid Off' : 'Active'}</span></td>
          <td>
            ${d.Status !== 3 ? `<button class="btn-primary-gradient" style="padding: 5px 12px; font-size: 0.8rem;" onclick="repayDebt('${d.Id}')">Repay</button>` : '<span style="color: #64748b;">Settled</span>'}
          </td>
        </tr>
      `).join('');
    } else {
      tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: #64748b;">No debts or loans tracked.</td></tr>';
    }
  } catch (err) {
    console.error('Failed to load debts', err);
  }
}

// 7. Load Obligations
async function loadObligations() {
  try {
    const res = await fetch(`${API_BASE}/obligations`, {
      headers: { 'X-Family-Id': state.familyId }
    });
    const items = await res.json();
    const tbody = document.getElementById('obligationsTableBody');
    if (items.length > 0) {
      tbody.innerHTML = items.map(o => `
        <tr>
          <td><strong style="color: #fff;">${o.Title}</strong></td>
          <td>${o.PayeeName}</td>
          <td style="font-weight: 600; color: #ef4444;">₹${Number(o.AmountDue).toLocaleString('en-IN')}</td>
          <td style="color: #94a3b8;">${new Date(o.DueDate).toLocaleDateString()}</td>
          <td><span class="badge-tag" style="background: rgba(239,68,68,0.2); color: #ef4444;">Priority: High</span></td>
          <td><span class="badge-tag" style="background: rgba(255,255,255,0.08);">${o.Status === 3 ? 'Paid' : 'Pending'}</span></td>
        </tr>
      `).join('');
    } else {
      tbody.innerHTML = '<tr><td colspan="6" style="text-align: center; color: #64748b;">No payment obligations recorded.</td></tr>';
    }
  } catch (err) {
    console.error('Failed to load obligations', err);
  }
}

// 8. Load Reports
async function loadReports() {
  try {
    const res = await fetch(`${API_BASE}/reports/summary`, {
      headers: { 'X-Family-Id': state.familyId }
    });
    const data = await res.json();
    renderReportsCategoryChart(data.categorySpending || []);
    renderReportsMemberChart(data.memberSpending || []);
  } catch (err) {
    console.error('Failed to load reports', err);
  }
}

function renderReportsCategoryChart(items) {
  const ctx = document.getElementById('reportsCategoryChart')?.getContext('2d');
  if (!ctx) return;
  if (state.charts.reportsCategory) state.charts.reportsCategory.destroy();

  state.charts.reportsCategory = new Chart(ctx, {
    type: 'bar',
    data: {
      labels: items.map(i => i.Name),
      datasets: [{
        label: 'Expense Amount (₹)',
        data: items.map(i => i.TotalAmount),
        backgroundColor: items.map(i => i.ColorHex || '#6366f1'),
        borderRadius: 6
      }]
    },
    options: {
      responsive: true,
      plugins: { legend: { display: false } },
      scales: {
        x: { ticks: { color: '#94a3b8' } },
        y: { ticks: { color: '#94a3b8' } }
      }
    }
  });
}

function renderReportsMemberChart(items) {
  const ctx = document.getElementById('reportsMemberChart')?.getContext('2d');
  if (!ctx) return;
  if (state.charts.reportsMember) state.charts.reportsMember.destroy();

  state.charts.reportsMember = new Chart(ctx, {
    type: 'pie',
    data: {
      labels: items.map(i => i.MemberName),
      datasets: [{
        data: items.map(i => i.TotalSpent),
        backgroundColor: ['#6366f1', '#ec4899', '#10b981', '#f59e0b']
      }]
    },
    options: {
      responsive: true,
      plugins: {
        legend: { position: 'bottom', labels: { color: '#94a3b8' } }
      }
    }
  });
}

// Modals
function openExpenseModal() { document.getElementById('expenseModal').style.display = 'flex'; }
function openIncomeModal() { document.getElementById('incomeModal').style.display = 'flex'; }
function closeModal(id) { document.getElementById(id).style.display = 'none'; }

// Action Handlers
async function handleCreateExpense(e) {
  e.preventDefault();
  const title = document.getElementById('expTitle').value;
  const amount = document.getElementById('expAmount').value;
  const categoryId = document.getElementById('expCategory').value;
  const paymentMethod = document.getElementById('expMethod').value;

  try {
    const res = await fetch(`${API_BASE}/expenses/create`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Family-Id': state.familyId },
      body: JSON.stringify({ title, amount, categoryId, paymentMethod })
    });
    if (res.ok) {
      closeModal('expenseModal');
      document.getElementById('expenseForm').reset();
      if (state.currentTab === 'expenses') loadExpenses();
      loadDashboard();
    }
  } catch (err) {
    alert('Failed to save expense');
  }
}

async function handleCreateIncome(e) {
  e.preventDefault();
  const title = document.getElementById('incTitle').value;
  const amount = document.getElementById('incAmount').value;
  const categoryId = document.getElementById('incCategory').value;
  const paymentMethod = document.getElementById('incMethod').value;

  try {
    const res = await fetch(`${API_BASE}/incomes/create`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Family-Id': state.familyId },
      body: JSON.stringify({ title, amount, categoryId, paymentMethod })
    });
    if (res.ok) {
      closeModal('incomeModal');
      document.getElementById('incomeForm').reset();
      if (state.currentTab === 'incomes') loadIncomes();
      loadDashboard();
    }
  } catch (err) {
    alert('Failed to save income');
  }
}

async function deleteExpense(id) {
  if (!confirm('Are you sure you want to delete this expense?')) return;
  try {
    const res = await fetch(`${API_BASE}/expenses/${id}`, { method: 'DELETE' });
    if (res.ok) {
      loadExpenses();
      loadDashboard();
    }
  } catch (err) {
    alert('Failed to delete expense');
  }
}

async function markBillPaid(occurrenceId, amount) {
  if (!confirm(`Mark this bill of ₹${amount} as paid?`)) return;
  try {
    const res = await fetch(`${API_BASE}/bills/mark-paid`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ occurrenceId, amountPaid: amount, paymentMethod: 'UPI' })
    });
    if (res.ok) {
      loadBills();
      loadDashboard();
    }
  } catch (err) {
    alert('Failed to settle bill');
  }
}

async function repayDebt(debtId) {
  const amountStr = prompt('Enter repayment installment amount (₹):', '5000');
  if (!amountStr || isNaN(Number(amountStr))) return;
  const amount = Number(amountStr);

  try {
    const res = await fetch(`${API_BASE}/debts/repay`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ debtId, amount, paymentMethod: 'UPI' })
    });
    if (res.ok) {
      loadDebts();
      loadDashboard();
    }
  } catch (err) {
    alert('Failed to record repayment');
  }
}
