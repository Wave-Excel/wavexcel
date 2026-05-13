#!/usr/bin/env node
/**
 * Build README.pdf with Mermaid flowcharts rendered (SVG inside the PDF).
 *
 * Usage (from my-project/docs):
 *   npm install
 *   npm run pdf
 *
 * Requires network (CDN for Mermaid; first Puppeteer install downloads Chromium).
 */
import { readFileSync, writeFileSync } from "fs";
import { dirname, join } from "path";
import { fileURLToPath } from "url";
import { marked } from "marked";
import puppeteer from "puppeteer";

const __dirname = dirname(fileURLToPath(import.meta.url));
const docsRoot = join(__dirname, "..");
const readmePath = join(docsRoot, "README.md");
const outPdfPath = join(docsRoot, "README.pdf");
const cacheHtmlPath = join(docsRoot, ".readme-pdf-cache.html");

const MERMAID_FENCE =
  /^```mermaid\s*\r?\n([\s\S]*?)^```\s*?\r?\n?/gm;

/** Minimal escape for text inside `<div class="mermaid">` (do not escape `>` — breaks `-->` arrows). */
function escapeHtml(text) {
  return text.replace(/&/g, "&amp;").replace(/</g, "&lt;");
}

/**
 * `marked` ends raw HTML blocks on a blank line. Mermaid diagrams often contain blank lines,
 * so injecting `<div class="mermaid">...</div>` into MD breaks the diagram text.
 * Strategy: substitute one-line placeholders, run `marked.parse`, then splice real `<div class="mermaid">`.
 */
function extractMermaidBlocks(markdown) {
  /** @type {string[]} */
  const blocks = [];
  const stripped = markdown.replace(MERMAID_FENCE, (_, body) => {
    blocks.push(body.replace(/\s+$/u, "").trimEnd());
    const idx = blocks.length - 1;
    return `\n\n<!--WXPDF-MERMAID-${idx}-->\n\n`;
  });
  return { stripped, blocks };
}

marked.setOptions({
  gfm: true,
  breaks: false,
});

const raw = readFileSync(readmePath, "utf8");
const { stripped: mdSkeleton, blocks: mermaidBlocks } = extractMermaidBlocks(raw);

let bodyHtml = marked.parse(mdSkeleton);
for (let i = 0; i < mermaidBlocks.length; i++) {
  const token = `<!--WXPDF-MERMAID-${i}-->`;
  const inner = escapeHtml(mermaidBlocks[i]);
  const diagram = `<div class="mermaid-diagram-wrapper"><pre class="mermaid">${inner}</pre></div>`;
  if (!bodyHtml.includes(token)) {
    console.warn(
      `WARN: Markdown engine did not preserve Mermaid placeholder ${i}; diagram may be missing.`
    );
  }
  bodyHtml = bodyHtml.split(token).join(diagram);
}

const html = `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>SST-200 Documentation</title>
  <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.5.0/github-markdown.min.css" />
  <style>
    @page { margin: 12mm 10mm; }
    body { padding: 1rem; box-sizing: border-box; }
    .markdown-body { box-sizing: border-box; max-width: none !important; font-size: 10pt; line-height: 1.42; }
    .markdown-body pre { font-size: 8.5pt; }
    .markdown-body pre:not(.mermaid) { }

    /* Mermaid: constrain size on A4 PDF (diagrams default to fairly large SVGs). */
    .mermaid-diagram-wrapper {
      margin: 0.65rem auto;
      padding: 0.35rem 0;
      text-align: center;
      overflow: visible;
      page-break-inside: avoid;
      max-width: 100%;
    }
    /* pre.mermaid keeps newlines in source; slim code-block chrome after SVG render */
    pre.mermaid {
      margin: 0 auto !important;
      padding: 0 !important;
      background: transparent !important;
      border: none !important;
      overflow: visible;
      text-align: center;
      font-family: ui-monospace, monospace;
      font-size: 12px;
      line-height: 1.25;
      white-space: pre-wrap;
      max-width: 100%;
      display: block;
    }

    /* Shrink rendered SVG — height cap keeps wide flowcharts usable on PDF pages */
    .mermaid-diagram-wrapper svg {
      display: inline-block !important;
      vertical-align: top;
      max-width: 100% !important;
      width: auto !important;
      max-height: 240px !important;
      height: auto !important;
    }
    /* Fallback if Mermaid still injects prose error text */
    .mermaid-diagram-wrapper .syntax-error,
    foreignObject[class*="error"] { font-size: 8px !important; }
  </style>
</head>
<body>
  <article class="markdown-body">
${bodyHtml}
  </article>
  <script src="https://cdn.jsdelivr.net/npm/mermaid@11.14/dist/mermaid.min.js"></script>
  <script>
    (async function () {
      try {
        mermaid.initialize({
          startOnLoad: false,
          theme: "neutral",
          suppressErrorRendering: true,
          securityLevel: "loose",
          flowchart: {
            htmlLabels: true,
            curve: "basis",
            padding: 6,
            nodeSpacing: 28,
            rankSpacing: 38,
          },
          themeVariables: {
            fontFamily: "ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif",
            fontSize: "11px",
            primaryTextColor: "#1a1a1a",
          },
          fontFamily: "ui-sans-serif, system-ui, -apple-system, Segoe UI, sans-serif",
        });
        await mermaid.run({ querySelector: "pre.mermaid" });
      } catch (e) {
        console.error(e);
      }
      window.__PDF_READY__ = true;
    })();
  </script>
</body>
</html>`;

writeFileSync(cacheHtmlPath, html, "utf8");

const browser = await puppeteer.launch({
  headless: true,
  args: ["--no-sandbox", "--disable-setuid-sandbox"],
});

try {
  const page = await browser.newPage();
  await page.setViewport({
    width: 1040,
    height: 1400,
    deviceScaleFactor: 1,
  });

  await page.goto(`file://${cacheHtmlPath}`, {
    waitUntil: "networkidle0",
    timeout: 180000,
  });

  await page.waitForFunction(
    () =>
      typeof window.__PDF_READY__ !== "undefined" &&
      window.__PDF_READY__ === true,
    { timeout: 180000 }
  );

  await new Promise((r) => setTimeout(r, 2500));
  await page
    .evaluate(() => {
      document.querySelectorAll("pre.mermaid svg").forEach((svg) => {
        svg.style.maxHeight = "220px";
        svg.style.maxWidth = "96%";
        svg.style.width = "auto";
        svg.style.height = "auto";
      });
    })
    .catch(() => {});

  await page.emulateMediaType("print");

  await page.pdf({
    path: outPdfPath,
    format: "A4",
    printBackground: true,
    margin: { top: "12mm", right: "10mm", bottom: "14mm", left: "10mm" },
    scale: 0.92,
    displayHeaderFooter: true,
    headerTemplate: "<span></span>",
    footerTemplate:
      '<div style="font-size:9px;width:100%;text-align:center;color:#555;padding:0 10mm;font-family:sans-serif;"><span class="pageNumber"></span> / <span class="totalPages"></span></div>',
  });

  console.log("Wrote:", outPdfPath);
  console.log(`Mermaid blocks embedded: ${mermaidBlocks.length}`);
} finally {
  await browser.close();
}
