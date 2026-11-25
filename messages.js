const API_BASE_URL = window.location.hostname === 'localhost'
  ? 'http://localhost:5242'
  : 'https://fdikrhevv2.execute-api.ap-southeast-2.amazonaws.com/prod';

const LOCAL_STORAGE_KEY = '3rd-space-published-messages';

class MessageStorageStrategy {
  async getAll(_page, _pageSize) {
    throw new Error('getAll must be implemented');
  }

  async send(_text) {
    throw new Error('send must be implemented');
  }
}

class LocalStorageMessageStrategy extends MessageStorageStrategy {
  async getAll(page, pageSize) {
    const response = await fetch(`${API_BASE_URL}/api/messages?page=${page}&pageSize=${pageSize}`);
    if (!response.ok) throw new Error('Unable to load messages');

    const data = await response.json();
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(data.items));
    return data;
  }

  async send(text, author) {
    const response = await fetch(`${API_BASE_URL}/api/messages`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ text, author: author || null })
    });

    if (!response.ok) {
      const problem = await response.json().catch(() => null);
      throw new Error(problem?.detail || 'Unable to send message');
    }

    const message = await response.json();
    const cached = JSON.parse(localStorage.getItem(LOCAL_STORAGE_KEY) || '[]');
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify([message, ...cached].slice(0, 500)));
    return message;
  }
}

const storage = new LocalStorageMessageStrategy();
const pageSize = 10;
let currentPage = 1;

async function loadMessages(page = 1) {
  const list = document.getElementById('messageList');
  const pagination = document.getElementById('pagination');

  list.textContent = 'Loading messages…';
  pagination.textContent = '';

  try {
    const data = await storage.getAll(page, pageSize);
    currentPage = data.page;

    if (data.items.length === 0) {
      list.innerHTML = '<p class="empty-state">No messages yet.</p>';
    } else {
      list.innerHTML = data.items.map(message => `
        <article class="message-card">
          <p class="message-author">${escapeHtml(message.author || 'Anonymous')}</p>
          <p>${escapeHtml(message.text)}</p>
          <time datetime="${message.createdAt}">${new Date(message.createdAt).toLocaleString()}</time>
        </article>
      `).join('');
    }

    pagination.innerHTML = '';
    for (let pageNumber = 1; pageNumber <= data.totalPages; pageNumber += 1) {
      const button = document.createElement('button');
      button.type = 'button';
      button.textContent = String(pageNumber);
      button.classList.toggle('active', pageNumber === data.page);
      button.setAttribute('aria-current', pageNumber === data.page ? 'page' : 'false');
      button.addEventListener('click', () => loadMessages(pageNumber));
      pagination.appendChild(button);
    }
  } catch (error) {
    list.textContent = 'Could not load messages. Is the API running?';
    console.error(error);
  }
}

function escapeHtml(value) {
  const element = document.createElement('div');
  element.textContent = value;
  return element.innerHTML;
}

document.getElementById('messageForm').addEventListener('submit', async event => {
  event.preventDefault();

  const input = document.getElementById('messageText');
  const authorInput = document.getElementById('messageAuthor');
  const status = document.getElementById('messageStatus');
  const text = input.value.trim();
  const author = authorInput.value.trim();

  if (!text) return;

  status.textContent = 'Sending…';
  try {
    await storage.send(text, author);
    input.value = '';
    authorInput.value = '';
    status.textContent = 'Message sent.';
    await loadMessages(1);
  } catch (error) {
    status.textContent = error.message;
  }
});

loadMessages(currentPage);
