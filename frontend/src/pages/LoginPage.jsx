import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { login } from "../api";
import { useAuth } from "../context/AuthContext";

export function LoginPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { setSession } = useAuth();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    setLoading(true);
    setError("");

    try {
      const result = await login(email, password);

      if (!["employee", "admin", "superadmin"].includes(result.role)) {
        throw new Error("Este panel requiere perfil employee, admin o superadmin.");
      }

      // Sincronizar evento de la URL si existe
      const urlEventId = searchParams.get("eventId");
      if (urlEventId) {
        localStorage.setItem("activeEventId", urlEventId);
        localStorage.setItem("selectedEventId", urlEventId);
      }

      setSession({
        token: result.token,
        user: {
          userId: result.userId,
          employeeId: result.employeeId,
          email: result.email,
          fullName: result.fullName,
          role: result.role,
          activeEvent: result.activeEvent ?? null,
        },
      });

      // Si hay un eventId en la URL, pasarlo también al dashboard
      const dashboardPath = urlEventId 
        ? `/dashboard?eventId=${encodeURIComponent(urlEventId)}`
        : "/dashboard";

      navigate(dashboardPath, { replace: true });
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="orbix-login-wrapper">
      {/* Luces de brillo ambiental */}
      <div className="orbix-login-glow-cyan" />
      <div className="orbix-login-glow-lime" />

      <section className="orbix-login-card">
        {/* Logo de Orbix */}
        <div className="orbix-logo">
          <svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg" style={{ width: "42px", height: "42px" }}>
            <circle 
              cx="50" 
              cy="50" 
              r="38" 
              stroke="url(#orbixGrad)" 
              strokeWidth="9" 
              fill="none" 
              strokeLinecap="round" 
              strokeDasharray="160 80" 
            />
            <circle cx="50" cy="50" r="14" fill="#009990" opacity="0.88" />
            <defs>
              <linearGradient id="orbixGrad" x1="0%" y1="0%" x2="100%" y2="100%">
                <stop offset="0%" stopColor="#001A6E" />
                <stop offset="58%" stopColor="#009990" />
                <stop offset="100%" stopColor="#E1FFBB" />
              </linearGradient>
            </defs>
          </svg>
          <span>Orbix</span>
        </div>

        <h1 className="orbix-title">Bienvenido a Orbix</h1>
        <p className="orbix-subtitle">Ingresa a tu cuenta de control de acceso</p>

        <form onSubmit={handleSubmit} className="orbix-form">
          <div className="orbix-group">
            <label className="orbix-label" htmlFor="email">Correo electrónico</label>
            <div className="orbix-input-container">
              <span className="orbix-input-icon">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
              </span>
              <input
                id="email"
                type="email"
                className="orbix-input"
                placeholder="nombre@correo.com"
                autoComplete="username"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
              />
            </div>
          </div>

          <div className="orbix-group">
            <label className="orbix-label" htmlFor="password">Contraseña</label>
            <div className="orbix-input-container">
              <span className="orbix-input-icon">
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                  <rect x="3" y="11" width="18" height="11" rx="2" ry="2" />
                  <path d="M7 11V7a5 5 0 0 1 10 0v4" />
                </svg>
              </span>
              <input
                id="password"
                type="password"
                className="orbix-input"
                placeholder="••••••••"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
            </div>
            <a href="#forgot" className="orbix-forgot-link" onClick={(e) => e.preventDefault()}>
              ¿Olvidaste tu contraseña?
            </a>
          </div>

          {error ? <p className="error-text" style={{ textAlign: "center", marginTop: "0.2rem" }}>{error}</p> : null}

          <button type="submit" className="orbix-btn" disabled={loading}>
            {loading ? "Iniciando sesion..." : "Ingresar"}
          </button>
        </form>

      </section>
    </div>
  );
}
