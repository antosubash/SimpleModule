import React from 'react';
import { useCurrentFrame, interpolate } from 'remotion';
import { colors, fonts } from '../theme';

type Props = {
  code: string;
  language?: 'csharp' | 'http' | 'typescript';
  fontSize?: number;
  revealStart?: number;
  revealDuration?: number;
  label?: string;
};

const csharpKeywords = new Set([
  'public', 'class', 'static', 'void', 'async', 'await', 'var', 'return',
  'new', 'this', 'null', 'using', 'record', 'struct', 'interface',
  'string', 'int', 'bool', 'decimal', 'Guid',
]);

const csharpTypes = new Set([
  'IEndpoint', 'IEndpointRouteBuilder', 'IModule', 'IServiceCollection',
  'IProductsContracts', 'CancellationToken', 'ProductsPermissions',
  'ProductsModule', 'UsersModule', 'OrdersModule', 'Product',
  'Module', 'Dto', 'ListProducts', 'ProductEndpoints', 'MapGet',
  'MapPost', 'MapPut', 'MapDelete', 'MapCrud', 'Inertia', 'OrderPlaced',
  'RequirePermission', 'AddModule', 'AddModules', 'PublishAsync',
  'GetAllAsync', 'Render',
]);

const tokenise = (line: string): Array<{ t: string; type: string }> => {
  const tokens: Array<{ t: string; type: string }> = [];
  const re =
    /(\/\/[^\n]*)|("(?:[^"\\]|\\.)*")|(\b\d+\b)|(\[[A-Za-z]+(?:\([^\)]*\))?\])|([A-Za-z_][A-Za-z0-9_]*)|([\s]+)|([^\sA-Za-z0-9_]+)/g;
  let m: RegExpExecArray | null;
  while ((m = re.exec(line)) !== null) {
    if (m[1]) tokens.push({ t: m[1], type: 'comment' });
    else if (m[2]) tokens.push({ t: m[2], type: 'string' });
    else if (m[3]) tokens.push({ t: m[3], type: 'number' });
    else if (m[4]) tokens.push({ t: m[4], type: 'attr' });
    else if (m[5]) {
      const w = m[5];
      if (csharpKeywords.has(w)) tokens.push({ t: w, type: 'keyword' });
      else if (csharpTypes.has(w)) tokens.push({ t: w, type: 'type' });
      else tokens.push({ t: w, type: 'ident' });
    } else if (m[6]) tokens.push({ t: m[6], type: 'space' });
    else if (m[7]) tokens.push({ t: m[7], type: 'punct' });
  }
  return tokens;
};

const colorFor = (type: string) => {
  switch (type) {
    case 'keyword': return colors.code.keyword;
    case 'type': return colors.code.type;
    case 'string': return colors.code.string;
    case 'comment': return colors.code.comment;
    case 'number': return colors.code.number;
    case 'attr': return colors.code.attr;
    case 'punct': return '#67e8f9';
    default: return colors.code.text;
  }
};

export const CodeBlock: React.FC<Props> = ({
  code,
  fontSize = 26,
  revealStart = 0,
  revealDuration = 40,
  label,
}) => {
  const frame = useCurrentFrame();
  const lines = code.split('\n');
  const perLine = Math.max(2, revealDuration / lines.length);

  return (
    <div
      style={{
        background: colors.code.bg,
        border: `1px solid ${colors.code.border}`,
        borderRadius: 18,
        padding: '28px 32px',
        fontFamily: fonts.mono,
        fontSize,
        lineHeight: 1.55,
        color: colors.code.text,
        boxShadow: `0 20px 60px rgba(0,0,0,0.45), inset 0 0 0 1px ${colors.accent}33`,
        position: 'relative',
        overflow: 'visible',
      }}
    >
      {label ? (
        <div
          style={{
            position: 'absolute',
            top: 10,
            right: 16,
            fontFamily: fonts.sans,
            fontSize: 14,
            fontWeight: 600,
            color: colors.inkMuted,
            textTransform: 'uppercase',
            letterSpacing: 1.2,
          }}
        >
          {label}
        </div>
      ) : null}
      {lines.map((line, i) => {
        const start = revealStart + i * perLine;
        const opacity = interpolate(
          frame,
          [start, start + perLine * 0.9],
          [0, 1],
          { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
        );
        const translate = interpolate(
          frame,
          [start, start + perLine],
          [8, 0],
          { extrapolateLeft: 'clamp', extrapolateRight: 'clamp' },
        );
        const tokens = tokenise(line || ' ');
        return (
          <div
            key={i}
            style={{
              opacity,
              transform: `translateY(${translate}px)`,
              whiteSpace: 'pre',
            }}
          >
            {tokens.map((tok, j) => (
              <span key={j} style={{ color: colorFor(tok.type) }}>
                {tok.t}
              </span>
            ))}
          </div>
        );
      })}
    </div>
  );
};
