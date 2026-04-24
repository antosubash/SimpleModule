export default function Welcome() {
  return (
    <div style={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: 'calc(100vh - 8rem)',
      fontFamily: "'DM Sans', system-ui, -apple-system, sans-serif",
      background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
      color: 'white',
      margin: '-2rem -1.5rem -4rem',
      padding: '4rem 1.5rem',
      borderRadius: '1.5rem',
    }}>
      <div style={{ textAlign: 'center', maxWidth: '600px', padding: '2rem' }}>
        <div style={{
          width: '80px',
          height: '80px',
          borderRadius: '20px',
          background: 'rgba(255,255,255,0.15)',
          backdropFilter: 'blur(10px)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          margin: '0 auto 1.5rem',
          fontSize: '2rem',
          fontWeight: 'bold',
        }}>
          ✦
        </div>
        <h1 style={{ fontSize: '3rem', fontWeight: 800, margin: '0 0 0.5rem', letterSpacing: '-0.02em' }}>
          Marketplace
        </h1>
        <p style={{ fontSize: '1.125rem', opacity: 0.85, margin: '0 0 2.5rem', lineHeight: 1.7 }}>
          Your modular monolith is running. Start building!
        </p>
        <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'center', flexWrap: 'wrap' }}>
          <a
            href="/swagger"
            style={{
              padding: '0.75rem 1.75rem',
              borderRadius: '12px',
              background: 'rgba(255,255,255,0.2)',
              backdropFilter: 'blur(10px)',
              color: 'white',
              textDecoration: 'none',
              fontWeight: 600,
              fontSize: '0.875rem',
              border: '1px solid rgba(255,255,255,0.3)',
              transition: 'all 0.2s',
            }}
          >
            API Docs →
          </a>
          <a
            href="/api/items"
            style={{
              padding: '0.75rem 1.75rem',
              borderRadius: '12px',
              background: 'rgba(255,255,255,0.1)',
              backdropFilter: 'blur(10px)',
              color: 'white',
              textDecoration: 'none',
              fontWeight: 600,
              fontSize: '0.875rem',
              border: '1px solid rgba(255,255,255,0.15)',
              transition: 'all 0.2s',
            }}
          >
            Items API →
          </a>
        </div>
        <p style={{ fontSize: '0.8rem', opacity: 0.5, marginTop: '3rem' }}>
          Run <code style={{ background: 'rgba(255,255,255,0.15)', padding: '0.15rem 0.5rem', borderRadius: '4px', fontSize: '0.75rem' }}>sm new module {'<name>'}</code> to add more modules
        </p>
      </div>
    </div>
  );
}