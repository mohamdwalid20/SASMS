/**
 * Eva Project - Main JavaScript
 * Handles all interactive elements and UI logic.
 */

document.addEventListener('DOMContentLoaded', () => {
    initMobileMenu();
    initStickyHeader();
    initSmoothScroll();
    initTabs();
    initHeroSlideshow();
    initStaffCarousel();
    initScrollAnimations();
});

/* =========================================
   1. MOBILE MENU
   ========================================= */
function initMobileMenu() {
    const menuBtn = document.querySelector('.mobile-menu-btn');
    const navbar = document.querySelector('.navbar');
    const navLinks = document.querySelectorAll('.navbar a');

    if (!menuBtn || !navbar) return;

    // Toggle menu
    menuBtn.addEventListener('click', () => {
        navbar.classList.toggle('active');
        const icon = menuBtn.querySelector('i');
        if (navbar.classList.contains('active')) {
            icon.classList.remove('fa-bars');
            icon.classList.add('fa-times');
        } else {
            icon.classList.remove('fa-times');
            icon.classList.add('fa-bars');
        }
    });

    // Close menu when clicking a link
    navLinks.forEach(link => {
        link.addEventListener('click', () => {
            navbar.classList.remove('active');
            const icon = menuBtn.querySelector('i');
            icon.classList.remove('fa-times');
            icon.classList.add('fa-bars');
        });
    });
}

/* =========================================
   2. STICKY HEADER
   ========================================= */
function initStickyHeader() {
    const header = document.querySelector('header');
    
    window.addEventListener('scroll', () => {
        if (window.scrollY > 50) {
            header.classList.add('scrolled');
        } else {
            header.classList.remove('scrolled');
        }
    });
}

/* =========================================
   3. SMOOTH SCROLL (WITH OFFSET)
   ========================================= */
function initSmoothScroll() {
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            const targetId = this.getAttribute('href');
            if (targetId === '#') return;

            const targetElement = document.querySelector(targetId);
            if (targetElement) {
                e.preventDefault();
                const headerOffset = 80; // Height of header
                const elementPosition = targetElement.getBoundingClientRect().top;
                const offsetPosition = elementPosition + window.pageYOffset - headerOffset;

                window.scrollTo({
                    top: offsetPosition,
                    behavior: "smooth"
                });
            }
        });
    });
}

/* =========================================
   4. TABS (CURRICULUM)
   ========================================= */
function initTabs() {
    const tabBtns = document.querySelectorAll('.tab-btn');
    const tabContents = document.querySelectorAll('.tab-content');

    if (!tabBtns.length) return;

    tabBtns.forEach(btn => {
        btn.addEventListener('click', () => {
            // Remove active class from all
            tabBtns.forEach(b => b.classList.remove('active'));
            tabContents.forEach(c => c.classList.remove('active'));

            // Add active to current
            btn.classList.add('active');
            const tabId = btn.getAttribute('data-tab');
            const targetContent = document.getElementById(tabId);
            if (targetContent) {
                targetContent.classList.add('active');
            }
        });
    });
}

/* =========================================
   5. HERO SLIDESHOW
   ========================================= */
function initHeroSlideshow() {
    const slides = document.querySelectorAll('.slide');
    if (slides.length < 2) return;

    let currentSlide = 0;
    const intervalTime = 5000; // 5 seconds

    setInterval(() => {
        // Remove active from current
        slides[currentSlide].classList.remove('active');
        
        // Next slide
        currentSlide = (currentSlide + 1) % slides.length;
        
        // Add active to new
        slides[currentSlide].classList.add('active');
    }, intervalTime);
}

/* =========================================
   6. STAFF CAROUSEL (INFINITE SCROLL)
   ========================================= */
function initStaffCarousel() {
    const track = document.querySelector('.staff-track');
    const carousel = document.querySelector('.staff-carousel-wrapper');
    
    if (!track || !carousel) return;

    // Clone items for infinite effect if not enough items
    // (CSS animation handles the scrolling, this ensures we have enough content width)
    const items = track.innerHTML;
    // Clone twice to ensure we fill the screen + buffer
    track.innerHTML = items + items + items;
}

/* =========================================
   7. SCROLL ANIMATIONS (INTERSECTION OBSERVER)
   ========================================= */
function initScrollAnimations() {
    const revealElements = document.querySelectorAll('.section-title, .card, .spec-card, .step-card, .gallery-item');
    
    const revealOptions = {
        threshold: 0.1,
        rootMargin: "0px 0px -50px 0px"
    };

    const revealOnScroll = new IntersectionObserver((entries, revealOnScroll) => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) return;
            
            entry.target.classList.add('reveal'); // Base class
            
            // Small delay for stagger effect if siblings
            setTimeout(() => {
                entry.target.classList.add('active');
            }, 100);
            
            revealOnScroll.unobserve(entry.target);
        });
    }, revealOptions);

    revealElements.forEach(el => {
        el.classList.add('reveal'); // Set initial state
        revealOnScroll.observe(el);
    });
}
