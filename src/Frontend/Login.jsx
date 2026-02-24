import React from "react";
import logo from "../assets/logo.png";
import "../styles/login.css";

const Login = () => {
  return (
    <div className="container">
      {/* Left Side */}
      <div className="left-section">
        <div className="brand">
          <div className="logo-icon">✉</div>
          <span>MediCare <small>SMART OPD</small></span>
        </div>

        <div className="left-content">
          <h1>Streamline Your Healthcare Operations</h1>
          <p>
            Manage appointments, patients, and OPD queues
            efficiently with our intelligent healthcare platform.
          </p>
        </div>
      </div>

      {/* Right Side */}
      <div className="right-section">
        <div className="login-card">
          <img src={logo} alt="MediCare Logo" className="logo" />

          <h2>Welcome Back 👋</h2>
          <p className="subtitle">Please login to your account</p>

          <form>
            <label>Email Address</label>
            <input type="email" placeholder="Enter your email" />

            <label>Password</label>
            <input type="password" placeholder="Enter your password" />

            <div className="options">
              <div>
                <input type="checkbox" />
                <span>Remember me</span>
              </div>
              <a href="#">Forgot password?</a>
            </div>

            <button type="submit">Login</button>

            <p className="signup">
              Don't have an account? <a href="#">Sign up</a>
            </p>
          </form>
        </div>
      </div>
    </div>
  );
};

export default Login;
