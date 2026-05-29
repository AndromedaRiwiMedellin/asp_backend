import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { getDashboard, getScanEvents } from "../api";
import { useAuth } from "../context/AuthContext";
import { ScannerPage } from "./ScannerPage";

function StatCard({ label, value }) {
  return (
    <article className="card stat-card">
      <p className="stat-label">{label}</p>
      <p className="stat-value">{value}</p>
    </article>
  );
}

export function DashboardPage() {
  const { token, user, logout } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();

  const getInitialEventId = () => {
    const urlEventId = searchParams.get("eventId");
    if (urlEventId) return urlEventId;

    const storedActive = localStorage.getItem("activeEventId");
    if (storedActive) return storedActive;

    const storedSelected = localStorage.getItem("selectedEventId");
    if (storedSelected) return storedSelected;

    return user?.activeEvent?.eventId ?? "";
  };

  const [tab, setTab] = useState("dashboard");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [dashboard, setDashboard] = useState(null);
  const [events, setEvents] = useState([]);
  const [selectedEventId, setSelectedEventId] = useState(getInitialEventId());

  const fullName = useMemo(() => user?.fullName ?? "Usuario", [user]);

  const loadEvents = async () => {
    try {
      const items = await getScanEvents(token);
      setEvents(items);

      if (!selectedEventId && items.length > 0) {
        const fallbackEventId = items[0].eventId;
        setSelectedEventId(fallbackEventId);
      }
    } catch (err) {
      setError(err.message);
    }
  };

  const loadDashboard = async (eventIdToUse = selectedEventId) => {
    setLoading(true);
    setError("");

    try {
      const payload = await getDashboard(token, eventIdToUse || undefined);
      setDashboard(payload);
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadEvents();
  }, []);

  useEffect(() => {
    if (selectedEventId) {
      localStorage.setItem("activeEventId", selectedEventId);
      localStorage.setItem("selectedEventId", selectedEventId);
      setSearchParams((prev) => {
        prev.set("eventId", selectedEventId);
        return prev;
      }, { replace: true });
    }
  }, [selectedEventId]);

  useEffect(() => {
    if (tab === "dashboard") {
      loadDashboard();
    }
  }, [tab, selectedEventId]);

  const selectedEvent = events.find((item) => item.eventId === selectedEventId);
  const activeEventLabel = selectedEvent?.eventTitle
    ?? dashboard?.session?.activeEvent?.eventTitle
    ?? user?.activeEvent?.eventTitle
    ?? "Sin evento";

  return (
    <main className="dashboard-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">Panel de control</p>
          <h1>{fullName}</h1>
          <p className="muted">Perfil: {user?.role ?? "user"}</p>
          <p className="muted">Evento activo: {activeEventLabel}</p>
          {user?.role === "superadmin" ? (
            <p className="helper-text">Modo superadmin: acceso habilitado sin datos operativos por ahora.</p>
          ) : null}
          <p className="helper-text">Para cambiar entre admin y empleado, primero cierra sesión.</p>
        </div>
        <button onClick={logout} className="ghost-button">Cerrar sesión</button>
      </header>

      <section className="card">
        <label htmlFor="event-selector">Evento para escaneo</label>
        <select
          id="event-selector"
          value={selectedEventId}
          onChange={(e) => setSelectedEventId(e.target.value)}
        >
          <option value="">Seleccionar evento automático</option>
          {events.map((ev) => (
            <option key={ev.eventId} value={ev.eventId}>
              {ev.eventTitle}
            </option>
          ))}
        </select>
      </section>

      <nav className="tabs">
        <button className={tab === "dashboard" ? "active" : ""} onClick={() => setTab("dashboard")}>Dashboard</button>
        <button className={tab === "scanner" ? "active" : ""} onClick={() => setTab("scanner")}>Escáner</button>
      </nav>

      {tab === "dashboard" ? (
        <section className="content-grid">
          {loading ? <p className="muted">Cargando métricas...</p> : null}
          {error ? <p className="error-text">{error}</p> : null}

          {dashboard ? (
            <>
              <section className="stats-grid">
                <StatCard label="Escaneados" value={dashboard.stats.totalScans} />
                <StatCard label="Escaneados hoy" value={dashboard.stats.scannedToday} />
                <StatCard label="Correctos" value={dashboard.stats.validScans} />
                <StatCard label="Fraudes" value={dashboard.stats.fraudScans} />
                <StatCard label="Ilegales" value={dashboard.stats.illegalScans} />
              </section>

              <article className="card table-card">
                <div className="table-header">
                  <h2>Últimos tickets escaneados</h2>
                  <button className="ghost-button" onClick={() => loadDashboard()}>Actualizar</button>
                </div>
                <div className="table-wrapper">
                  <table>
                    <thead>
                      <tr>
                        <th>Hora</th>
                        <th>Resultado</th>
                        <th>Razón</th>
                        <th>Propietario</th>
                        <th>Evento</th>
                        <th>Silla</th>
                        <th>QR</th>
                      </tr>
                    </thead>
                    <tbody>
                      {dashboard.recentScans.length === 0 ? (
                        <tr>
                          <td colSpan="7">Sin escaneos aún.</td>
                        </tr>
                      ) : (
                        dashboard.recentScans.map((scan) => (
                          <tr key={scan.scanId}>
                            <td>{scan.scannedAt ? new Date(scan.scannedAt).toLocaleString() : "-"}</td>
                            <td>{scan.success ? "Correcto" : "Rechazado"}</td>
                            <td>{scan.reason}</td>
                            <td>{scan.ownerName ?? "-"}</td>
                            <td>{scan.eventTitle ?? "-"}</td>
                            <td>{scan.seatNumber ?? "-"}</td>
                            <td>{scan.qrCode ?? "-"}</td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                </div>
              </article>
            </>
          ) : null}
        </section>
      ) : (
        <ScannerPage
          token={token}
          eventId={selectedEventId || null}
          session={dashboard?.session ?? null}
        />
      )}
    </main>
  );
}
