/* =========================================
   Pheidi Marketing Site — Chart.js Configs
   ========================================= */

document.addEventListener('DOMContentLoaded', () => {

  // ---- Shared defaults ----
  Chart.defaults.font.family = "'Lato', 'Helvetica Neue', sans-serif";
  Chart.defaults.font.size = 13;
  Chart.defaults.color = '#334752';
  Chart.defaults.plugins.legend.labels.usePointStyle = true;
  Chart.defaults.plugins.legend.labels.padding = 16;

  const PRIMARY = '#46B39D';
  const PRIMARY_LIGHT = 'rgba(70,179,157,0.15)';
  const ACCENT = '#E37332';
  const ACCENT_LIGHT = 'rgba(227,115,50,0.15)';
  const GRAY = '#bbb';
  const GRAY_LIGHT = 'rgba(187,187,187,0.15)';
  const RED = '#e74c3c';
  const RED_LIGHT = 'rgba(231,76,60,0.15)';
  const YELLOW = '#f39c12';
  const GREEN = '#4caf50';
  const GREEN_LIGHT = 'rgba(76,175,80,0.15)';

  // Lazy chart initialization via IntersectionObserver
  const chartMap = {};
  const canvases = document.querySelectorAll('canvas[id^="chart-"]');

  const chartObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting && !chartMap[entry.target.id]) {
        initChart(entry.target.id);
        chartObserver.unobserve(entry.target);
      }
    });
  }, { threshold: 0.2 });

  canvases.forEach(c => chartObserver.observe(c));

  function initChart(id) {
    const ctx = document.getElementById(id);
    if (!ctx) return;

    switch (id) {

      // ---- Feature 1: Missed Training Impact ----
      case 'chart-missed-training':
        chartMap[id] = new Chart(ctx, {
          type: 'bar',
          data: {
            labels: ['0 days', '7 days', '13 days', '21 days', '28+ days'],
            datasets: [{
              label: 'Race Time Slowdown (%)',
              data: [0, 4.25, 6, 7, 8],
              backgroundColor: [GREEN, '#8bc34a', YELLOW, ACCENT, RED],
              borderRadius: 6,
              maxBarThickness: 60,
            }]
          },
          options: {
            responsive: true,
            plugins: {
              legend: { display: false },
              tooltip: {
                callbacks: {
                  label: ctx => `~${ctx.parsed.y}% slower`
                }
              }
            },
            scales: {
              y: {
                beginAtZero: true,
                max: 10,
                title: { display: true, text: '% Race Time Slowdown' },
                grid: { color: '#f0f0f0' }
              },
              x: {
                title: { display: true, text: 'Days of Missed Training' },
                grid: { display: false }
              }
            }
          }
        });
        break;

      // ---- Feature 2: Adaptive Progression vs 10% Rule ----
      case 'chart-progression':
        const weeks = Array.from({length: 16}, (_, i) => `W${i+1}`);
        const tenPercent = [25];
        const adaptive = [25];
        for (let i = 1; i < 16; i++) {
          tenPercent.push(Math.round(tenPercent[i-1] * 1.10));
          // Adaptive: higher increase at low vol, lower at high, with deloads
          const isDeload = (i % 4 === 3);
          if (isDeload) {
            adaptive.push(Math.round(adaptive[i-1] * 0.85));
          } else {
            const rate = adaptive[i-1] < 35 ? 1.14 : adaptive[i-1] < 50 ? 1.10 : 1.07;
            adaptive.push(Math.round(adaptive[i-1] * rate));
          }
        }

        chartMap[id] = new Chart(ctx, {
          type: 'line',
          data: {
            labels: weeks,
            datasets: [
              {
                label: 'Traditional 10% Rule',
                data: tenPercent,
                borderColor: GRAY,
                backgroundColor: GRAY_LIGHT,
                borderDash: [6, 4],
                tension: 0.3,
                pointRadius: 3,
                fill: false,
              },
              {
                label: 'Adaptive Model',
                data: adaptive,
                borderColor: PRIMARY,
                backgroundColor: PRIMARY_LIGHT,
                tension: 0.3,
                pointRadius: 3,
                fill: true,
              }
            ]
          },
          options: {
            responsive: true,
            plugins: {
              tooltip: {
                callbacks: {
                  label: ctx => `${ctx.dataset.label}: ${ctx.parsed.y} km/week`
                }
              }
            },
            scales: {
              y: {
                beginAtZero: false,
                title: { display: true, text: 'Weekly km' },
                grid: { color: '#f0f0f0' }
              },
              x: { grid: { display: false } }
            }
          }
        });
        break;

      // ---- Feature 3: Gray Zone Problem ----
      case 'chart-gray-zone':
        chartMap[id] = new Chart(ctx, {
          type: 'bar',
          data: {
            labels: ['Typical Runner', 'Optimal 80/20', 'Your Pheidi Plan'],
            datasets: [
              {
                label: 'Zone 1 (Easy)',
                data: [50, 80, 80],
                backgroundColor: GREEN,
              },
              {
                label: 'Zone 2 (Gray Zone)',
                data: [40, 5, 5],
                backgroundColor: '#ddd',
              },
              {
                label: 'Zone 3 (Hard)',
                data: [10, 15, 15],
                backgroundColor: ACCENT,
              }
            ]
          },
          options: {
            responsive: true,
            plugins: {
              tooltip: {
                callbacks: {
                  label: ctx => `${ctx.dataset.label}: ${ctx.parsed.y}%`
                }
              }
            },
            scales: {
              x: { stacked: true, grid: { display: false } },
              y: {
                stacked: true,
                max: 100,
                title: { display: true, text: '% of Training Time' },
                grid: { color: '#f0f0f0' }
              }
            }
          }
        });
        break;

      // ---- Feature 5: C25K Wall ----
      case 'chart-c25k':
        chartMap[id] = new Chart(ctx, {
          type: 'line',
          data: {
            labels: ['W1', 'W2', 'W3', 'W4', 'W5', 'W6', 'W7', 'W8'],
            datasets: [
              {
                label: 'Traditional C25K',
                data: [1, 3, 5, 8, 13.8, 18, 22, 25],
                borderColor: RED,
                backgroundColor: RED_LIGHT,
                tension: 0.3,
                pointRadius: 4,
                fill: false,
              },
              {
                label: 'Pheidi Approach',
                data: [1, 1.5, 2.3, 3.4, 5.1, 7.7, 11.5, 17],
                borderColor: PRIMARY,
                backgroundColor: PRIMARY_LIGHT,
                tension: 0.3,
                pointRadius: 4,
                fill: true,
              }
            ]
          },
          options: {
            responsive: true,
            plugins: {
              tooltip: {
                callbacks: {
                  label: ctx => `${ctx.dataset.label}: ${ctx.parsed.y} min continuous`
                }
              }
            },
            scales: {
              y: {
                beginAtZero: true,
                title: { display: true, text: 'Continuous Running (min)' },
                grid: { color: '#f0f0f0' }
              },
              x: {
                title: { display: true, text: 'Week' },
                grid: { display: false }
              }
            }
          }
        });
        break;

      // ---- Feature 6a: Recovery by Age ----
      case 'chart-recovery-age':
        chartMap[id] = new Chart(ctx, {
          type: 'bar',
          data: {
            labels: ['Under 40', '40–49', '50–59', '60+'],
            datasets: [{
              label: 'Recovery Hours',
              data: [30, 42, 54, 66],
              backgroundColor: [GREEN, '#8bc34a', YELLOW, ACCENT],
              borderRadius: 6,
              maxBarThickness: 50,
            }]
          },
          options: {
            indexAxis: 'y',
            responsive: true,
            plugins: {
              legend: { display: false },
              tooltip: {
                callbacks: {
                  label: ctx => `${ctx.parsed.x} hours avg recovery`
                }
              }
            },
            scales: {
              x: {
                beginAtZero: true,
                max: 80,
                title: { display: true, text: 'Hours to Full Recovery' },
                grid: { color: '#f0f0f0' }
              },
              y: { grid: { display: false } }
            }
          }
        });
        break;

      // ---- Feature 6b: VO2max Decline ----
      case 'chart-vo2max':
        const ages = [25, 30, 35, 40, 45, 50, 55, 60, 65, 70, 75];
        const sedentary = [50, 48, 45, 41, 37, 33, 29, 25, 22, 19, 16];
        const active = [52, 51, 50, 48, 46, 44, 42, 39, 37, 35, 33];

        chartMap[id] = new Chart(ctx, {
          type: 'line',
          data: {
            labels: ages,
            datasets: [
              {
                label: 'Sedentary',
                data: sedentary,
                borderColor: GRAY,
                borderDash: [6, 4],
                tension: 0.3,
                pointRadius: 3,
                fill: false,
              },
              {
                label: 'Consistent Runners',
                data: active,
                borderColor: PRIMARY,
                backgroundColor: PRIMARY_LIGHT,
                tension: 0.3,
                pointRadius: 3,
                fill: true,
              }
            ]
          },
          options: {
            responsive: true,
            scales: {
              y: {
                title: { display: true, text: 'VO2max (ml/kg/min)' },
                grid: { color: '#f0f0f0' }
              },
              x: {
                title: { display: true, text: 'Age' },
                grid: { display: false }
              }
            }
          }
        });
        break;

      // ---- Feature 7: ACWR Risk Zones ----
      case 'chart-acwr':
        const acwrLabels = [];
        const greenData = [];
        const yellowData = [];
        const redData = [];
        for (let r = 0.5; r <= 2.0; r += 0.1) {
          acwrLabels.push(r.toFixed(1));
          const inGreen = (r >= 0.8 && r <= 1.3);
          const inYellow = (r > 1.3 && r <= 1.5);
          const inRed = r > 1.5;
          // Risk curve approximation
          const risk = r <= 0.8 ? 25 + (0.8 - r) * 30
                     : r <= 1.3 ? 15
                     : r <= 1.5 ? 15 + (r - 1.3) * 150
                     : 45 + (r - 1.5) * 80;
          greenData.push(inGreen ? Math.round(risk) : null);
          yellowData.push(inYellow ? Math.round(risk) : null);
          redData.push(inRed ? Math.round(risk) : null);
        }

        // Fill missing edges for visual continuity
        const allRisk = acwrLabels.map((_, i) => {
          const r = 0.5 + i * 0.1;
          return r <= 0.8 ? 25 + (0.8 - r) * 30
               : r <= 1.3 ? 15
               : r <= 1.5 ? 15 + (r - 1.3) * 150
               : 45 + (r - 1.5) * 80;
        });

        chartMap[id] = new Chart(ctx, {
          type: 'line',
          data: {
            labels: acwrLabels,
            datasets: [
              {
                label: 'Sweet Spot (0.8–1.3)',
                data: allRisk.map((v, i) => {
                  const r = 0.5 + i * 0.1;
                  return r >= 0.75 && r <= 1.35 ? v : null;
                }),
                borderColor: GREEN,
                backgroundColor: GREEN_LIGHT,
                fill: true,
                tension: 0.4,
                pointRadius: 0,
                spanGaps: false,
              },
              {
                label: 'Caution (1.3–1.5)',
                data: allRisk.map((v, i) => {
                  const r = 0.5 + i * 0.1;
                  return r >= 1.25 && r <= 1.55 ? v : null;
                }),
                borderColor: YELLOW,
                backgroundColor: 'rgba(243,156,18,0.15)',
                fill: true,
                tension: 0.4,
                pointRadius: 0,
                spanGaps: false,
              },
              {
                label: 'Danger (1.5+)',
                data: allRisk.map((v, i) => {
                  const r = 0.5 + i * 0.1;
                  return r >= 1.45 ? v : null;
                }),
                borderColor: RED,
                backgroundColor: RED_LIGHT,
                fill: true,
                tension: 0.4,
                pointRadius: 0,
                spanGaps: false,
              },
              {
                label: 'Under-training',
                data: allRisk.map((v, i) => {
                  const r = 0.5 + i * 0.1;
                  return r <= 0.85 ? v : null;
                }),
                borderColor: GRAY,
                backgroundColor: GRAY_LIGHT,
                fill: true,
                tension: 0.4,
                pointRadius: 0,
                spanGaps: false,
              }
            ]
          },
          options: {
            responsive: true,
            scales: {
              y: {
                beginAtZero: true,
                title: { display: true, text: 'Relative Injury Risk' },
                grid: { color: '#f0f0f0' }
              },
              x: {
                title: { display: true, text: 'ACWR Ratio' },
                grid: { display: false }
              }
            }
          }
        });
        break;

      // ---- Feature 9: Taper Volume ----
      case 'chart-taper':
        chartMap[id] = new Chart(ctx, {
          type: 'bar',
          data: {
            labels: ['3 Weeks Out', '2 Weeks Out', '1 Week Out', 'Race Week'],
            datasets: [
              {
                label: 'Marathon',
                data: [100, 80, 60, 35],
                backgroundColor: PRIMARY,
                borderRadius: 4,
              },
              {
                label: 'Half Marathon',
                data: [100, 85, 65, 45],
                backgroundColor: ACCENT,
                borderRadius: 4,
              },
              {
                label: '5K / 10K',
                data: [100, 90, 75, 55],
                backgroundColor: '#64b5f6',
                borderRadius: 4,
              }
            ]
          },
          options: {
            responsive: true,
            plugins: {
              tooltip: {
                callbacks: {
                  label: ctx => `${ctx.dataset.label}: ${ctx.parsed.y}% of peak volume`
                }
              }
            },
            scales: {
              y: {
                beginAtZero: true,
                max: 110,
                title: { display: true, text: '% of Peak Volume' },
                grid: { color: '#f0f0f0' }
              },
              x: { grid: { display: false } }
            }
          }
        });
        break;

      // ---- Feature 12: Heat Pace Adjustment ----
      case 'chart-heat':
        chartMap[id] = new Chart(ctx, {
          type: 'line',
          data: {
            labels: [10, 15, 20, 25, 30, 35],
            datasets: [{
              label: 'Pace Adjustment (sec/km)',
              data: [0, 0, 7.5, 15, 22.5, 30],
              borderColor: ACCENT,
              backgroundColor: ACCENT_LIGHT,
              tension: 0.3,
              pointRadius: 5,
              pointBackgroundColor: ACCENT,
              fill: true,
            }]
          },
          options: {
            responsive: true,
            plugins: {
              legend: { display: false },
              tooltip: {
                callbacks: {
                  label: ctx => {
                    if (ctx.parsed.y === 0) return 'No adjustment';
                    return `+${ctx.parsed.y} sec/km slower`;
                  }
                }
              }
            },
            scales: {
              y: {
                beginAtZero: true,
                title: { display: true, text: 'Pace Adjustment (sec/km)' },
                grid: { color: '#f0f0f0' }
              },
              x: {
                title: { display: true, text: 'Temperature (°C)' },
                grid: { display: false }
              }
            }
          }
        });
        break;
    }
  }
});
