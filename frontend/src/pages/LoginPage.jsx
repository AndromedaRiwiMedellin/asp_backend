import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { login } from "../api";
import { useAuth } from "../context/AuthContext";

export function LoginPage() {
  const navigate = useNavigate();
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

      navigate("/dashboard", { replace: true });
    } catch (err) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <main className="login-shell">
      <section className="card login-card">
        <p className="eyebrow">Andromeda Control</p>
        <h1>Ingreso de personal (Admin / Empleado)</h1>
        <p className="muted">Al iniciar sesión verás quién eres y qué evento estás escaneando.</p>

        <form onSubmit={handleSubmit} className="form">
          <label htmlFor="email">Correo</label>
          <input
            id="email"
            type="email"
            autoComplete="username"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />

          <label htmlFor="password">Contraseña</label>
          <input
            id="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />

          {error ? <p className="error-text">{error}</p> : null}

          <button type="submit" disabled={loading}>
            {loading ? "Ingresando..." : "Entrar al dashboard"}
          </button>
        </form>

        <p className="helper-text">Si hay una sesión activa de otro usuario, cierra sesión para cambiar a admin o empleado.</p>
      </section>
    </main>
  );
}
