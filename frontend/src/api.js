const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "/api";

async function request(path, options = {}) {
  const response = await fetch(`${API_BASE}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(options.headers || {}),
    },
    ...options,
  });

  let payload = null;
  try {
    payload = await response.json();
  } catch {
    payload = null;
  }

  if (!response.ok) {
    const message = payload?.message || "Error en la solicitud";
    throw new Error(message);
  }

  return payload;
}

export async function login(email, password) {
  return request("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password }),
  });
}

export async function getScanEvents(token) {
  return request("/tickets/events", {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function getDashboard(token, eventId) {
  const query = eventId ? `?eventId=${encodeURIComponent(eventId)}` : "";
  return request(`/tickets/dashboard${query}`, {
    headers: { Authorization: `Bearer ${token}` },
  });
}

export async function scanTicket(token, qrCode, eventId) {
  return request("/tickets/scan", {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: JSON.stringify({ qrCode, eventId: eventId || null }),
  });
}
