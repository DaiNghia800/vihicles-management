function token() {
    return $('input[name=__RequestVerificationToken]').val();
}

function loginAccount(userInput) {
    userInput.__RequestVerificationToken = token();
    Swal.fire({
        title: 'Logging in...',
        text: 'Please wait a moment.',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    $.ajax({
        type: "POST",
        url: "/Account/LoginToSystem",
        data: userInput,
        dataType: 'json',
        success: function (res) {
            Swal.close();
            if (res.status === 'success' && res.success === true) {
                Swal.fire({
                    icon: "success",
                    title: "Login Successful!",
                    text: res.message,
                    timer: 1500,
                    showConfirmButton: false
                }).then(() => {
                    location.href = res.redirectUrl || '/';
                });
            }
            else {
                Swal.fire({
                    icon: "error",
                    title: "Login Failed",
                    text: res.message
                });
            }
        },
        error: function (xhr, status, error) {
            Swal.close();
            console.error('Login error:', xhr.responseText);
            Swal.fire({
                icon: "error",
                title: "Connection Error",
                text: "Unable to connect to server"
            });
        }
    });
}

function signupAccount(userInput) {
    userInput.__RequestVerificationToken = token();
    Swal.fire({
        title: 'Signing up...',
        text: 'Please wait a moment.',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });
    $.ajax({
        type: "POST",
        url: "/Account/SignUpUser",
        data: userInput,
        dataType: 'json',
        success: function (res) {
            Swal.close();
            if (res.success) {
                Swal.fire({
                    icon: "success",
                    title: "Sign Up Successful!",
                    text: res.message,
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    window.location.href = '/login';
                });
            } else {
                let errorMessage = res.message;
                if (res.errors && res.errors.length > 0) {
                    errorMessage += '\n\n' + res.errors.join('\n');
                }
                Swal.fire({
                    icon: "error",
                    title: "Sign Up Failed",
                    text: errorMessage
                });
            }
        },
        error: function (xhr, status, error) {
            Swal.close();
            console.error('Signup error:', xhr.responseText);
            Swal.fire({
                icon: "error",
                title: "Connection Error",
                text: "Unable to connect to server. Please try again."
            });
        }
    });
}

// Send OTP
function sendOtp(email) {
    var tokenValue = token();
    Swal.fire({
        title: 'Sending OTP...',
        text: 'Please wait a moment.',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    $.ajax({
        type: "POST",
        url: "/Account/SendOtp",
        data: {
            email: email,
            __RequestVerificationToken: tokenValue
        },
        dataType: 'json',
        success: function (res) {
            Swal.close();
            if (res.success) {
                Swal.fire({
                    icon: "success",
                    title: "Success",
                    text: res.message,
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    sessionStorage.setItem('resetEmail', email);
                    window.location.href = '/otp';
                });
            } else {
                Swal.fire({
                    icon: "error",
                    title: "Error",
                    text: res.message
                });
            }
        },
        error: function (xhr, status, error) {

            try {
                var errorResponse = JSON.parse(xhr.responseText);
                console.log('Parsed Error Response:', errorResponse);
            } catch (e) {
                console.log('Could not parse error response');
            }

            Swal.close();
            var errorMessage = "Unable to connect to server";
            Swal.fire({
                icon: "error",
                title: "Connection Error",
                text: errorMessage
            });
        }
    });
}

// Verify OTP
function verifyOtp(email, otpCode) {
    Swal.fire({
        title: 'Verifying OTP...',
        text: 'Please wait a moment.',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    $.ajax({
        type: "POST",
        url: "/Account/VerifyOtp",
        data: {
            email: email,
            otpCode: otpCode,
            __RequestVerificationToken: token()
        },
        dataType: 'json',
        success: function (res) {
            Swal.close();
            if (res.success) {
                Swal.fire({
                    icon: "success",
                    title: "Success",
                    text: res.message,
                    timer: 1500,
                    showConfirmButton: false
                }).then(() => {
                    sessionStorage.setItem('otpCode', otpCode);
                    window.location.href = '/reset-password';
                });
            } else {
                Swal.fire({
                    icon: "error",
                    title: "Error",
                    text: res.message
                });
            }
        },
        error: function (xhr, status, error) {
            Swal.close();
            console.error('Verify OTP error:', xhr.responseText);
            Swal.fire({
                icon: "error",
                title: "Connection Error",
                text: "Unable to connect to server"
            });
        }
    });
}

// Reset password
function resetPassword(email, otpCode, newPassword, confirmPassword) {
    Swal.fire({
        title: 'Resetting password...',
        text: 'Please wait a moment.',
        allowOutsideClick: false,
        didOpen: () => {
            Swal.showLoading();
        }
    });

    $.ajax({
        type: "POST",
        url: "/Account/ResetPassword",
        data: {
            email: email,
            otpCode: otpCode,
            newPassword: newPassword,
            confirmPassword: confirmPassword,
            __RequestVerificationToken: token()
        },
        dataType: 'json',
        success: function (res) {
            Swal.close();
            if (res.success) {
                Swal.fire({
                    icon: "success",
                    title: "Success",
                    text: res.message,
                    timer: 2000,
                    showConfirmButton: false
                }).then(() => {
                    sessionStorage.removeItem('resetEmail');
                    sessionStorage.removeItem('otpCode');
                    window.location.href = '/login';
                });
            } else {
                Swal.fire({
                    icon: "error",
                    title: "Error",
                    text: res.message
                });
            }
        },
        error: function (xhr, status, error) {
            Swal.close();
            console.error('Reset password error:', xhr.responseText);
            Swal.fire({
                icon: "error",
                title: "Connection Error",
                text: "Unable to connect to server"
            });
        }
    });
}

// Handle OTP inputs
function initOtpInputs() {
    const otpInputs = $('.otp-input');

    // Handle input
    otpInputs.on('input', function () {
        const input = $(this);
        const value = input.val();

        // Only allow numbers
        if (!/^\d*$/.test(value)) {
            input.val('');
            return;
        }

        // Auto focus to next field
        if (value.length === 1) {
            const nextIndex = parseInt(input.attr('data-index')) + 1;
            if (nextIndex < 6) {
                $(`.otp-input[data-index="${nextIndex}"]`).focus();
            }
        }

        // Update hidden input
        updateOtpCode();
    });

    // Handle backspace
    otpInputs.on('keydown', function (e) {
        if (e.key === 'Backspace' && $(this).val() === '') {
            const prevIndex = parseInt($(this).attr('data-index')) - 1;
            if (prevIndex >= 0) {
                $(`.otp-input[data-index="${prevIndex}"]`).focus();
            }
        }
    });

    // Handle paste
    otpInputs.first().on('paste', function (e) {
        e.preventDefault();
        const pastedData = e.originalEvent.clipboardData.getData('text');
        const digits = pastedData.match(/\d/g);

        if (digits && digits.length === 6) {
            otpInputs.each(function (index) {
                $(this).val(digits[index]);
            });
            updateOtpCode();
            otpInputs.last().focus();
        }
    });

    function updateOtpCode() {
        let otp = '';
        otpInputs.each(function () {
            otp += $(this).val();
        });
        $('#otp-code').val(otp);
    }
}

// Countdown timer for resend OTP
let countdownInterval = null;

function startResendCountdown(seconds) {
    const resendLink = $('#resend-otp-link');
    const countdownSpan = $('#countdown');
    let timeLeft = seconds;

    resendLink.css({
        'pointer-events': 'none',
        'opacity': '0.5'
    });

    // Clear old interval if exists
    if (countdownInterval) {
        clearInterval(countdownInterval);
    }

    countdownInterval = setInterval(() => {
        timeLeft--;
        countdownSpan.text(timeLeft);

        if (timeLeft <= 0) {
            clearInterval(countdownInterval);
            countdownInterval = null;
            resendLink.css({
                'pointer-events': 'auto',
                'opacity': '1'
            });
            resendLink.html('Resend It');
        }
    }, 1000);
}

document.addEventListener('DOMContentLoaded', function () {
    // Login form handler
    $('#login_form').off('submit').on('submit', function (e) {
        e.preventDefault();
        console.log('Login form submitted');
        loginAccount({
            username: $('#email').val(),
            password: $('#password').val(),
            rememberMe: $('#flexCheckDefault').is(':checked')
        });
        return false;
    });

    // Sign Up form handler
    $('#signup_form').off('submit').on('submit', function (e) {
        e.preventDefault();
        console.log('Signup form submitted');

        // Create form data to send to server
        const formData = {
            FullName: $('input[name="FullName"]').val().trim(),
            Email: $('input[name="Email"]').val().trim(),
            Password: $('input[name="Password"]').val(),
            ConfirmPassword: $('input[name="ConfirmPassword"]').val()
        };

        console.log('Signup data:', formData);
        signupAccount(formData);
        return false;
    });

    // Forgot Password form handler
    $('#forgot_password_form').off('submit').on('submit', function (e) {
        e.preventDefault();
        console.log('Forgot password form submitted');

        const email = $('#forgot-email').val();

        if (!email) {
            Swal.fire({
                icon: "warning",
                title: "Notice",
                text: "Please enter your email"
            });
            return false;
        }

        sendOtp(email);
        return false;
    });

    // Initialize OTP inputs if on OTP page
    if ($('#verify_otp_form').length > 0) {
        // Display masked email
        const email = sessionStorage.getItem('resetEmail');
        if (email) {
            const maskedEmail = email.substring(0, 3) + '*****' + email.substring(email.indexOf('@'));
            $('#masked-email').text(maskedEmail);
        }

        // Initialize OTP inputs
        initOtpInputs();

        // Start countdown
        startResendCountdown(60);

        // Focus on first field
        $('.otp-input').first().focus();

        // Handle OTP form submit
        $('#verify_otp_form').off('submit').on('submit', function (e) {
            e.preventDefault();
            console.log('Verify OTP form submitted');

            const email = sessionStorage.getItem('resetEmail');
            const otpCode = $('#otp-code').val();

            if (!email) {
                Swal.fire({
                    icon: "error",
                    title: "Error",
                    text: "Email not found. Please try again from the beginning."
                }).then(() => {
                    window.location.href = '/forgot-password';
                });
                return false;
            }

            if (!otpCode || otpCode.length !== 6) {
                Swal.fire({
                    icon: "warning",
                    title: "Notice",
                    text: "Please enter the complete OTP code (6 digits)"
                });
                return false;
            }

            verifyOtp(email, otpCode);
            return false;
        });

        // Resend OTP handler
        $('#resend-otp-link').off('click').on('click', function (e) {
            e.preventDefault();

            if ($(this).css('pointer-events') === 'none') {
                return;
            }

            const email = sessionStorage.getItem('resetEmail');

            if (!email) {
                Swal.fire({
                    icon: "error",
                    title: "Error",
                    text: "Email not found. Please try again from the beginning."
                }).then(() => {
                    window.location.href = '/forgot-password';
                });
                return;
            }

            Swal.fire({
                title: 'Resending OTP...',
                text: 'Please wait a moment.',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            $.ajax({
                type: "POST",
                url: "/Account/SendOtp",
                data: {
                    email: email,
                    __RequestVerificationToken: token()
                },
                dataType: 'json',
                success: function (res) {
                    Swal.close();
                    if (res.success) {
                        Swal.fire({
                            icon: "success",
                            title: "Success",
                            text: "A new OTP code has been sent to your email",
                            timer: 2000,
                            showConfirmButton: false
                        });
                        // Restart countdown
                        startResendCountdown(60);
                        // Clear OTP inputs
                        $('.otp-input').val('');
                        $('.otp-input').first().focus();
                        $('#otp-code').val('');
                    } else {
                        Swal.fire({
                            icon: "error",
                            title: "Error",
                            text: res.message
                        });
                    }
                },
                error: function (xhr, status, error) {
                    Swal.close();
                    console.error('Resend OTP error:', xhr.responseText);
                    Swal.fire({
                        icon: "error",
                        title: "Connection Error",
                        text: "Unable to connect to server"
                    });
                }
            });
        });
    }

    // Reset Password form handler
    $('#reset_password_form').off('submit').on('submit', function (e) {
        e.preventDefault();
        console.log('Reset password form submitted');

        const email = sessionStorage.getItem('resetEmail');
        const otpCode = sessionStorage.getItem('otpCode');
        const newPassword = $('#new-password').val();
        const confirmPassword = $('#confirm-password').val();

        if (!email || !otpCode) {
            Swal.fire({
                icon: "error",
                title: "Error",
                text: "Session has expired. Please try again from the beginning."
            }).then(() => {
                window.location.href = '/forgot-password';
            });
            return false;
        }

        if (!newPassword || !confirmPassword) {
            Swal.fire({
                icon: "warning",
                title: "Notice",
                text: "Please fill in all fields"
            });
            return false;
        }

        if (newPassword !== confirmPassword) {
            Swal.fire({
                icon: "warning",
                title: "Notice",
                text: "Passwords do not match"
            });
            return false;
        }

        resetPassword(email, otpCode, newPassword, confirmPassword);
        return false;
    });

    // External login buttons
    $('#google-signup-button').on('click', function (e) {
        e.preventDefault();
        $('#google-signup-form').submit();
    });

    $('#google-login-button').on('click', function (e) {
        e.preventDefault();
        $('#google-login-form').submit();
    });

    $('#facebook-signup-button').on('click', function (e) {
        e.preventDefault();
        $('#facebook-signup-form').submit();
    });

    $('#facebook-login-button').on('click', function (e) {
        e.preventDefault();
        $('#facebook-login-form').submit();
    });
});
//end Login and Sign Up

//Logout
document.addEventListener("DOMContentLoaded", function () {
    const logoutBtn = document.getElementById("btnLogout");
    if (!logoutBtn) return;

    logoutBtn.addEventListener("click", function (e) {
        e.preventDefault();

        Swal.fire({
            title: 'Are you sure you want to logout?',
            text: "Your session will end.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Log out',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                window.location.href = logoutBtn.getAttribute("href");
            }
        });
    });
});
//end logout

//login for user
$(document).ready(function () {
    // Toggle profile dropdown
    $('.profile-box .profile').on('click', function (e) {
        e.stopPropagation();
        $(this).siblings('.profile-dropdown').toggle();
    });

    // Close dropdown when clicking outside
    $(document).on('click', function (e) {
        if (!$(e.target).closest('.profile-box').length) {
            $('.profile-dropdown').hide();
        }
    });

    // Logout confirmation for frontend
    $(document).on('click', '#btnLogoutFrontend', function (e) {
        e.preventDefault();

        Swal.fire({
            title: 'Are you sure you want to logout?',
            text: "Your session will end.",
            icon: 'warning',
            showCancelButton: true,
            confirmButtonColor: '#3085d6',
            cancelButtonColor: '#d33',
            confirmButtonText: 'Log out',
            cancelButtonText: 'Cancel'
        }).then((result) => {
            if (result.isConfirmed) {
                window.location.href = $(this).attr('href');
            }
        });
    });
});
//end login for user