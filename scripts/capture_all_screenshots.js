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

  // 01 Login
  console.log('1. Navigating to Login page...');
  await page.goto('http://127.0.0.1:5000/login', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, '01_login.png') });
  console.log('Captured 01_login.png');

  // Login
  console.log('2. Logging in...');
  await page.locator('input[type="email"]').fill('admin@redisgui.local');
  await page.locator('input[type="password"]').fill('AdminPassword123!');
  await page.locator('button[type="submit"]').click();
  await page.waitForTimeout(2500);

  // 02 Dashboard
  console.log('3. Capturing Dashboard...');
  await page.waitForTimeout(2000);
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, '02_dashboard.png') });
  console.log('Captured 02_dashboard.png');

  // 14 Add Connection Dialog
  console.log('3b. Opening Add Connection Dialog on Dashboard...');
  const addConnBtn = page.locator('button:has-text("Add Connection"), .btn-add-connection, button:has(mat-icon:has-text("add"))').first();
  if (await addConnBtn.isVisible()) {
    await addConnBtn.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '14_add_connection_dialog.png') });
    console.log('Captured 14_add_connection_dialog.png');
    await page.keyboard.press('Escape');
    await page.waitForTimeout(1000);
  }

  // Navigate to storage
  console.log('4. Navigating to Storage Manager...');
  const connId = 'ae3201ab-c3f1-4594-8f80-bbffd8d5eb65';
  await page.goto(`http://127.0.0.1:5000/storage/${connId}`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(3000);

  // 03 Storage Table View
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, '03_storage_table.png') });
  console.log('Captured 03_storage_table.png');

  // 04 Storage Tree View
  console.log('5. Switching to Tree View...');
  const treeToggleBtn = page.locator('app-toggler button, .toggler-btn, button:has-text("Tree")').first();
  if (await treeToggleBtn.isVisible()) {
    await treeToggleBtn.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '04_storage_tree.png') });
    console.log('Captured 04_storage_tree.png');
    // Switch back to table view
    const tableToggleBtn = page.locator('app-toggler button, .toggler-btn, button:has-text("Table"), button:has-text("List")').first();
    if (await tableToggleBtn.isVisible()) {
      await tableToggleBtn.click();
      await page.waitForTimeout(1500);
    }
  }

  // 05 Stream Visual Editor
  console.log('6. Opening Stream Key...');
  const streamChip = page.locator('.type-chip:has-text("Stream"), button:has-text("Stream")').first();
  if (await streamChip.isVisible()) {
    await streamChip.click();
    await page.waitForTimeout(1000);
  }
  const streamRow = page.locator('tr.data-row').first();
  if (await streamRow.isVisible()) {
    await streamRow.click();
    await page.waitForTimeout(2500);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '05_stream_visual_editor.png') });
    console.log('Captured 05_stream_visual_editor.png');

    // 06 Stream JSON Editor
    console.log('7. Toggling Stream to JSON view...');
    const visualToggle = page.locator('mat-slide-toggle, .mdc-switch').first();
    if (await visualToggle.isVisible()) {
      await visualToggle.click();
      await page.waitForTimeout(1500);
      await page.screenshot({ path: path.join(SCREENSHOT_DIR, '06_stream_json_editor.png') });
      console.log('Captured 06_stream_json_editor.png');
    }

    // Close editor
    const closeBtn = page.locator('.panel-actions button:has(mat-icon:has-text("close")), button:has(mat-icon:has-text("close"))').first();
    if (await closeBtn.isVisible()) {
      await closeBtn.click();
      await page.waitForTimeout(1000);
    }
  }

  // 07 Hash Key Editor
  console.log('8. Opening Hash Key...');
  const hashChip = page.locator('.type-chip:has-text("Hash"), button:has-text("Hash")').first();
  if (await hashChip.isVisible()) {
    await hashChip.click();
    await page.waitForTimeout(1000);
  }
  const hashRow = page.locator('tr.data-row').first();
  if (await hashRow.isVisible()) {
    await hashRow.click();
    await page.waitForTimeout(2500);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '07_hash_editor.png') });
    console.log('Captured 07_hash_editor.png');

    const closeBtn = page.locator('.panel-actions button:has(mat-icon:has-text("close")), button:has(mat-icon:has-text("close"))').first();
    if (await closeBtn.isVisible()) {
      await closeBtn.click();
      await page.waitForTimeout(1000);
    }
  }

  // 07b String Key Editor
  console.log('8b. Opening String Key...');
  const strChip = page.locator('.type-chip:has-text("String"), button:has-text("String")').first();
  if (await strChip.isVisible()) {
    await strChip.click();
    await page.waitForTimeout(1000);
  }
  const strRow = page.locator('tr.data-row').first();
  if (await strRow.isVisible()) {
    await strRow.click();
    await page.waitForTimeout(2500);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '07b_string_editor.png') });
    console.log('Captured 07b_string_editor.png');

    const closeBtn = page.locator('.panel-actions button:has(mat-icon:has-text("close")), button:has(mat-icon:has-text("close"))').first();
    if (await closeBtn.isVisible()) {
      await closeBtn.click();
      await page.waitForTimeout(1000);
    }
  }

  // Reset to All chip
  const allChip = page.locator('.chip-all, .type-chip:has-text("All")').first();
  if (await allChip.isVisible()) {
    await allChip.click();
    await page.waitForTimeout(1000);
  }

  // 15 Add Key Modal
  console.log('8c. Opening Add Key...');
  const addKeyBtn = page.locator('.add-key-btn, button:has-text("Add Key")').first();
  if (await addKeyBtn.isVisible()) {
    await addKeyBtn.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '15_add_key_dialog.png') });
    console.log('Captured 15_add_key_dialog.png');
    await page.keyboard.press('Escape');
    await page.waitForTimeout(1000);
  }

  // 16 Command Palette
  console.log('8d. Opening Command Palette...');
  const cmdPaletteBtn = page.locator('.cmd-shortcut-btn, button:has-text("Ctrl+K")').first();
  if (await cmdPaletteBtn.isVisible()) {
    await cmdPaletteBtn.click();
    await page.waitForTimeout(1000);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '16_command_palette.png') });
    console.log('Captured 16_command_palette.png');
    await page.keyboard.press('Escape');
    await page.waitForTimeout(1000);
  }

  // 08 Load Test Dialog
  console.log('9. Opening Load Test Dialog...');
  const loadTestBtn = page.locator('.loadtest-btn, button:has-text("Load Test")').first();
  if (await loadTestBtn.isVisible()) {
    await loadTestBtn.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '08_load_test_dialog.png') });
    console.log('Captured 08_load_test_dialog.png');
    await page.keyboard.press('Escape');
    await page.waitForTimeout(1000);
  }

  // 09 Seed Data Dialog
  console.log('10. Opening Seed Data Dialog...');
  const seedBtn = page.locator('.seed-btn, button:has-text("Seed Demo")').first();
  if (await seedBtn.isVisible()) {
    await seedBtn.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '09_seed_data_dialog.png') });
    console.log('Captured 09_seed_data_dialog.png');
    await page.keyboard.press('Escape');
    await page.waitForTimeout(1000);
  }

  // 10 Redis CLI Drawer
  console.log('11. Opening Redis CLI Drawer...');
  const cliTrigger = page.locator('app-redis-cli-drawer .cli-header, .cli-drawer-toggle, button:has-text("Terminal"), button:has-text("CLI")').first();
  if (await cliTrigger.isVisible()) {
    await cliTrigger.click();
    await page.waitForTimeout(1500);
    // Type a command
    const cliInput = page.locator('.cli-input input, .terminal-prompt input').first();
    if (await cliInput.isVisible()) {
      await cliInput.fill('INFO memory');
      await cliInput.press('Enter');
      await page.waitForTimeout(1000);
    }
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '10_redis_cli_drawer.png') });
    console.log('Captured 10_redis_cli_drawer.png');

    // Close CLI
    await cliTrigger.click();
    await page.waitForTimeout(1000);
  }

  // 11 Slowlog Dialog
  console.log('12. Opening Slowlog Dialog...');
  const slowlogBtn = page.locator('.slowlog-btn, button:has-text("Slowlog")').first();
  if (await slowlogBtn.isVisible()) {
    await slowlogBtn.click();
    await page.waitForTimeout(2000);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '11_slowlog_dialog.png') });
    console.log('Captured 11_slowlog_dialog.png');
    await page.keyboard.press('Escape');
    await page.waitForTimeout(1000);
  }

  // 12 Audit Logs
  console.log('13. Navigating to Audit Logs...');
  await page.goto('http://127.0.0.1:5000/audit-logs', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  await page.screenshot({ path: path.join(SCREENSHOT_DIR, '12_audit_logs.png') });
  console.log('Captured 12_audit_logs.png');

  // 13 Light Theme
  console.log('14. Capturing Light Theme on Storage...');
  await page.goto(`http://127.0.0.1:5000/storage/${connId}`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  const themeToggle = page.locator('button:has(mat-icon:has-text("light_mode")), button:has(mat-icon:has-text("dark_mode")), .theme-toggle').first();
  if (await themeToggle.isVisible()) {
    await themeToggle.click();
    await page.waitForTimeout(1500);
    await page.screenshot({ path: path.join(SCREENSHOT_DIR, '13_light_theme_preview.png') });
    console.log('Captured 13_light_theme_preview.png');
    // Toggle back to dark
    await themeToggle.click();
    await page.waitForTimeout(1000);
  }

  await browser.close();
  console.log('ALL screenshots captured successfully!');
}

run().catch(err => {
  console.error('Screenshot runner error:', err);
  process.exit(1);
});
