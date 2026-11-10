const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const SCREENSHOT_DIR = path.resolve(__dirname, '../docs/screenshots');
if (!fs.existsSync(SCREENSHOT_DIR)) {
  fs.mkdirSync(SCREENSHOT_DIR, { recursive: true });
}

async function run() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 1.5,
  });
  const page = await context.newPage();

  console.log('Navigating to login page...');
  await page.goto('http://127.0.0.1:5000/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);

  // 1. Capture Login Page
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, '01_login.png') });
  console.log('Captured 01_login.png');

  // Fill login
  console.log('Logging in...');
  const emailInput = page.locator('input[type="email"], input[formcontrolname="email"], input[name="email"]');
  const passwordInput = page.locator('input[type="password"], input[formcontrolname="password"], input[name="password"]');
  const submitBtn = page.locator('button[type="submit"]');

  await emailInput.fill('admin@example.com');
  await passwordInput.fill('AdminPassword123!');
  await submitBtn.click();

  await page.waitForTimeout(2000);
  console.log('Current URL after login:', page.url());

  // 2. Capture Dashboard
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, '02_dashboard.png') });
  console.log('Captured 02_dashboard.png');

  await browser.close();
}

run().catch(err => {
  console.error('Error running script:', err);
  process.exit(1);
});
