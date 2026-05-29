import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { login } from "../api";
import { useAuth } from "../context/AuthContext";

export function LoginPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { setSession } = useAuth();

  const [email, setEmail] = useState("empleado@andromeda.com");
  const [password, setPassword] = useState("Empleado123!");
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
      <div className="orbix-login-glow-purple" />

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
            <circle cx="50" cy="50" r="14" fill="#00d2ff" opacity="0.85" />
            <defs>
              <linearGradient id="orbixGrad" x1="0%" y1="0%" x2="100%" y2="100%">
                <stop offset="0%" stopColor="#00d2ff" />
                <stop offset="60%" stopColor="#33e0ff" />
                <stop offset="100%" stopColor="#9333ea" />
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
            {loading ? "Iniciando sesión..." : "Sign In"}
          </button>
        </form>

        <div className="orbix-divider">o ingresa con</div>

        <div className="orbix-social">
          <button className="orbix-social-btn" title="Iniciar con Google" disabled>
            <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" fill="#4285F4"/>
              <path d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" fill="#34A853"/>
              <path d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.06H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.94l2.85-2.22.81-.63z" fill="#FBBC05"/>
              <path d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.06l3.66 2.84c.87-2.6 3.3-4.52 6.16-4.52z" fill="#EA4335"/>
            </svg>
          </button>
          <button className="orbix-social-btn" title="Iniciar con Apple" disabled>
            <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.81-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M15.97 4.17c.66-.81 1.11-1.93.99-3.06-1 .04-2.21.67-2.93 1.49-.62.69-1.16 1.84-1.01 2.96 1.12.09 2.27-.57 2.95-1.39" fill="currentColor"/>
            </svg>
          </button>
        </div>

        <p className="orbix-signup-text">
          ¿No tienes una cuenta? <span className="orbix-signup-link">Regístrate</span>
        </p>
      </section>
    </div>
  );
}
