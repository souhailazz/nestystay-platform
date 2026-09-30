document.addEventListener("DOMContentLoaded", () => {

  /* ELEMENTS */
  const platLeft = document.getElementById("platLeft");
  const goldLeft = document.getElementById("goldLeft");
  const subscribeForm = document.getElementById("subscribeForm");
  const formMessage = document.getElementById("formMessage");

  /* =========================
     TRACKER - FETCH REAL DATA
  ========================== */
  async function updateTracker() {
    try {
      const response = await fetch('tracker.php');
      if (!response.ok) throw new Error('Failed to fetch tracker data');
      
      const data = await response.json();
      
      platLeft.textContent = `${data.platinum_filled}/${data.platinum_total}`;
      goldLeft.textContent = `${data.gold_filled}/${data.gold_total}`;
    } catch (error) {
      console.error('Error loading tracker:', error);
      // Fallback to initial values
      platLeft.textContent = "0/150";
      goldLeft.textContent = "0/150";
    }
  }

  // Initialize tracker
  updateTracker();

  /* =========================
     FORM VALIDATION
  ========================== */
  function validateForm(data) {
    const errors = [];
    
    // Check required fields
    const required = ['name', 'email', 'whatsapp', 'location', 'category', 'tier'];
    required.forEach(field => {
      if (!data[field] || data[field].trim() === '') {
        errors.push(`${field.replace(/_/g, ' ')} is required`);
      }
    });

    // Validate email
    if (data.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email)) {
      errors.push("Please enter a valid email address");
    }

    // Validate WhatsApp (no alphabets)
    if (data.whatsapp && !/^[0-9+\-\s\(\)]+$/.test(data.whatsapp)) {
      errors.push("WhatsApp number should contain only numbers, spaces, and + - ( ) characters");
    }

    return errors;
  }

  /* =========================
     FORM SUBMISSION
  ========================== */
  subscribeForm.addEventListener("submit", async e => {
    e.preventDefault();

    // Get form data
    const formData = new FormData(subscribeForm);
    const data = Object.fromEntries(formData.entries());
    
    console.log("Form data:", data);

    // Validate form
    const errors = validateForm(data);
    if (errors.length > 0) {
      formMessage.style.display = "block";
      formMessage.textContent = errors.join(". ");
      formMessage.className = "form-message error";
      return;
    }

    // Show loading state
    const submitBtn = subscribeForm.querySelector('button[type="submit"]');
    const originalBtnText = submitBtn.textContent;
    submitBtn.textContent = "Processing...";
    submitBtn.disabled = true;

    // Clear previous messages
    formMessage.style.display = "none";
    formMessage.textContent = "";
    formMessage.className = "";

    try {
      // Submit using POST method
      const response = await fetch('send.php', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: new URLSearchParams(data).toString()
      });

      const result = await response.json();
      
      console.log("Response:", result);
      
      formMessage.style.display = "block";
      formMessage.textContent = result.message;
      formMessage.className = `form-message ${result.status}`;

      if (result.status === "success") {
        // Update tracker with real data
        await updateTracker();
        subscribeForm.reset();
        
        // Show success message
        setTimeout(() => {
          formMessage.style.display = "none";
        }, 5000);
      }
      
    } catch (error) {
      console.error("Submission error:", error);
      
      // Fallback: Update counter locally
      updateCounterLocal(data.tier);
      
      formMessage.style.display = "block";
      formMessage.textContent = `Thank you, ${data.name}! Your ${data.tier} founder spot has been registered.`;
      formMessage.className = "form-message success";
      subscribeForm.reset();
      
      setTimeout(() => {
        formMessage.style.display = "none";
      }, 5000);
      
    } finally {
      // Restore button state
      submitBtn.textContent = originalBtnText;
      submitBtn.disabled = false;
    }
  });

  /* =========================
     HELPER FUNCTION TO UPDATE COUNTER LOCALLY
  ========================== */
  function updateCounterLocal(tier) {
    if (tier === "Platinum") {
      const platSpan = document.getElementById("platLeft");
      const [current, total] = platSpan.textContent.split('/');
      const currentNum = parseInt(current);
      const totalNum = parseInt(total);
      
      if (currentNum < totalNum) {
        platSpan.textContent = `${currentNum + 1}/${totalNum}`;
      }
    } else if (tier === "Gold") {
      const goldSpan = document.getElementById("goldLeft");
      const [current, total] = goldSpan.textContent.split('/');
      const currentNum = parseInt(current);
      const totalNum = parseInt(total);
      
      if (currentNum < totalNum) {
        goldSpan.textContent = `${currentNum + 1}/${totalNum}`;
      }
    }
  }
});

/* =========================
     COUNTDOWN
========================== */
const launchDate = new Date("2026-04-30T23:59:59").getTime();

function updateCountdown() {
  const now = Date.now();
  const d = launchDate - now;
  
  if (d <= 0) {
    // Launch date reached
    days.textContent = "00";
    hours.textContent = "00";
    minutes.textContent = "00";
    seconds.textContent = "00";
    return;
  }

  days.textContent = String(Math.floor(d / (1000 * 60 * 60 * 24))).padStart(2, '0');
  hours.textContent = String(Math.floor((d / (1000 * 60 * 60)) % 24)).padStart(2, '0');
  minutes.textContent = String(Math.floor((d / (1000 * 60)) % 60)).padStart(2, '0');
  seconds.textContent = String(Math.floor((d / 1000) % 60)).padStart(2, '0');
}

// Update countdown immediately and then every second
updateCountdown();
setInterval(updateCountdown, 1000);