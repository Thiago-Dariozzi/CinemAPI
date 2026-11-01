// Hook PostToolUse de Claude Code: después de cada Write/Edit formatea el archivo tocado
// y, en archivos JS/JSX, corre ESLint. Si quedan errores de lint, sale con código 2 para
// que Claude los vea y los corrija. Funciona en Windows (Git Bash), macOS y Linux.
import { spawnSync } from "node:child_process";
import { existsSync, readdirSync, readFileSync } from "node:fs";
import path from "node:path";

const isWindows = process.platform === "win32";

const readInput = () => {
  try {
    return JSON.parse(readFileSync(0, "utf8") || "{}");
  } catch {
    return {};
  }
};

const findUp = (startDir, match) => {
  let dir = startDir;
  for (;;) {
    const hit = match(dir);
    if (hit) return hit;
    const parent = path.dirname(dir);
    if (parent === dir) return null;
    dir = parent;
  }
};

const quote = (arg) => (isWindows && /[\s"]/.test(arg) ? `"${arg.replace(/"/g, '\\"')}"` : arg);

const run = (command, args, cwd) =>
  spawnSync(command, isWindows ? args.map(quote) : args, {
    cwd,
    encoding: "utf8",
    shell: isWindows,
    timeout: 55_000,
  });

const formatCSharp = (file) => {
  const project = findUp(path.dirname(file), (dir) => {
    const csproj = readdirSync(dir).find((name) => name.endsWith(".csproj"));
    return csproj ? path.join(dir, csproj) : null;
  });
  if (!project) return 0;
  const projectDir = path.dirname(project);
  const relativeFile = path.relative(projectDir, file);
  run("dotnet", ["format", "whitespace", project, "--include", relativeFile, "--verbosity", "quiet"], projectDir);
  return 0;
};

const lintJavaScript = (file) => {
  const root = findUp(path.dirname(file), (dir) => (existsSync(path.join(dir, "package.json")) ? dir : null));
  if (!root || !existsSync(path.join(root, "node_modules", ".bin"))) return 0;
  if (path.relative(root, file).split(path.sep).includes("CinemapiPhone")) return 0;

  const hasPrettier = existsSync(path.join(root, "node_modules", "prettier"));
  if (hasPrettier) run("npx", ["--no-install", "prettier", "--write", file], root);

  const hasEslint = existsSync(path.join(root, "node_modules", "eslint"));
  if (!hasEslint || !/\.(jsx?|mjs|cjs|tsx?)$/.test(file)) return 0;

  const result = run("npx", ["--no-install", "eslint", "--fix", file], root);
  if (result.status !== 0 && result.status !== null) {
    process.stderr.write(
      `ESLint encontró problemas en ${path.relative(root, file)} que no se pudieron corregir solos. ` +
        `Corregilos siguiendo las reglas del proyecto:\n${result.stdout}${result.stderr}`,
    );
    return 2;
  }
  return 0;
};

const input = readInput();
const file = input?.tool_input?.file_path;
if (!file || !existsSync(file)) process.exit(0);

const ext = path.extname(file).toLowerCase();
let code = 0;
try {
  if (ext === ".cs") code = formatCSharp(file);
  else if ([".js", ".jsx", ".mjs", ".cjs", ".ts", ".tsx", ".css", ".json"].includes(ext)) code = lintJavaScript(file);
} catch (error) {
  process.stderr.write(`Hook de formato: ${error.message}\n`);
  code = 0;
}
process.exit(code);
