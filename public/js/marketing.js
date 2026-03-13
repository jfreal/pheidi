/* =========================================
   Pheidi Marketing Site — Interactions
   ========================================= */

document.addEventListener('DOMContentLoaded', () => {

  // ---- Scroll-triggered fade-in animations ----
  const fadeEls = document.querySelectorAll('.fade-in');
  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        entry.target.classList.add('visible');
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.1, rootMargin: '0px 0px -40px 0px' });

  fadeEls.forEach(el => observer.observe(el));

  // ---- Navbar background on scroll ----
  const nav = document.getElementById('mainNav');
  const onScroll = () => {
    if (window.scrollY > 60) {
      nav.classList.add('scrolled');
    } else {
      nav.classList.remove('scrolled');
    }
  };
  window.addEventListener('scroll', onScroll, { passive: true });
  onScroll();

  // ---- Close mobile nav on link click ----
  const navLinks = document.querySelectorAll('#navbarNav .nav-link');
  const navCollapse = document.getElementById('navbarNav');
  navLinks.forEach(link => {
    link.addEventListener('click', () => {
      if (navCollapse.classList.contains('show')) {
        const bsCollapse = bootstrap.Collapse.getInstance(navCollapse);
        if (bsCollapse) bsCollapse.hide();
      }
    });
  });

  // ---- Active nav link highlighting on scroll ----
  const sections = document.querySelectorAll('section[id]');
  const navItems = document.querySelectorAll('#navbarNav .nav-link[href^="#"]');

  const highlightNav = () => {
    let current = '';
    sections.forEach(section => {
      const top = section.offsetTop - 120;
      if (window.scrollY >= top) {
        current = section.getAttribute('id');
      }
    });
    navItems.forEach(link => {
      link.classList.remove('active');
      if (link.getAttribute('href') === '#' + current) {
        link.classList.add('active');
      }
    });
  };

  window.addEventListener('scroll', highlightNav, { passive: true });
});
