//const { data } = require("jquery");

//according menu
const sidebarLink = document.querySelectorAll(".sidebar .sidebar-link");
if (sidebarLink.length > 0) {
    sidebarLink.forEach(item => {
        item.addEventListener("click", () => {
            const itemActive = document.querySelector(".sidebar .sidebar-link.active");
            if (itemActive) {
                itemActive.classList.remove("active");
            }

            const iconAccording = item.querySelector(".according-menu");
            if (iconAccording) {
                iconAccording.classList.toggle("active");
            }

            item.classList.add("active");
        })
    })
}
//end according menu

//close sidebar 
const buttonClose = document.querySelector(".toggle-sidebar");
if (buttonClose) {
    buttonClose.addEventListener("click", () => {
        const header = document.querySelector(".header");
        const sidebar = document.querySelector(".sidebar");

        header.classList.toggle("close");
        sidebar.classList.toggle("close");
    })
}
//end close sidebar

//slick
$(document).ready(function () {
    $('.slide-dashboard-category').slick({
        slidesToShow: 10,
        slidesToScroll: 1,
        responsive: [
            {
                breakpoint: 1500,
                settings: {
                    slidesToShow: 9
                }
            },
            {
                breakpoint: 1400,
                settings: {
                    slidesToShow: 8
                }
            },
            {
                breakpoint: 1280,
                settings: {
                    slidesToShow: 7
                }
            },
            {
                breakpoint: 1200,
                settings: {
                    slidesToShow: 6
                }
            }
        ]
    });
});
//end slick

//select2
$(document).ready(function () {
    $('.js-example-basic-single').select2({
        width: 'resolve'
    });
});
//end select2

//tinymce 
tinymce.init({
    selector: '#editor',
    license_key: 'gpl',
    plugins: 'lists link image table code wordcount',
    toolbar: 'undo redo | styleselect | bold italic | alignleft aligncenter alignright | code',
    height: 300,
    branding: true
});
//end tinymce

//dropzone
const dropzoneElement = document.querySelector("#my-dropzone");
if (dropzoneElement) {
    Dropzone.autoDiscover = false;
    let uploadedImages = [];
    const thumbnail = document.getElementById("Thumbnail");
    let dataThumbnail;
    if (thumbnail) {
        dataThumbnail = thumbnail.getAttribute("data-thumbnail");
    }

    const myDropzone = new Dropzone(dropzoneElement, {
        url: "/admin/upload/image",
        method: "post",
        paramName: 'files',
        autoProcessQueue: false,
        uploadMultiple: true,
        parallelUploads: 6,
        maxFilesize: 5,
        maxFiles: 6,
        acceptedFiles: "image/*",
        addRemoveLinks: true,
        headers: {
            "Cache-Control": null,
            "X-Requested-With": null
        },
        dictDefaultMessage: `
            <div class= "dz-message-inner">
                <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="feather feather-upload-cloud"><polyline points="16 16 12 12 8 16"></polyline><line x1="12" y1="12" x2="12" y2="21"></line><path d="M20.39 18.39A5 5 0 0 0 18 9h-1.26A8 8 0 1 0 3 16.3"></path><polyline points="16 16 12 12 8 16"></polyline></svg>
                <p>Drop files here or click to upload</p>
            </div >
        `,

        init: function () {
            var dz = this;

            if (dataThumbnail) {
                try {
                    uploadedImages = JSON.parse(dataThumbnail);
                } catch (e) {
                    uploadedImages = [];
                }
            }

            const buttonSave = document.querySelector("[button-save]");
            if (buttonSave) {
                buttonSave.addEventListener("click", function (e) {
                    e.preventDefault();

                    if (dz.getQueuedFiles().length > 0) {
                        dz.processQueue();
                    } else {
                        document.getElementById("Thumbnail").value = JSON.stringify(uploadedImages);
                        document.getElementById("mainForm").submit();
                    }
                });
            }

            this.on("sending", function (file, xhr, formData) {
                formData.append("files", file);
            });

            if (dataThumbnail !== null) {
                uploadedImages.forEach(function (url) {
                    var mockFile = { name: url.split("/").pop(), size: 12345, existingUrl: url };
                    dz.emit("addedfile", mockFile);
                    dz.emit("thumbnail", mockFile, url);
                    dz.emit("complete", mockFile);
                });
            }

            this.on("successmultiple", function (file, response) {
                if (response.urls) {
                    uploadedImages.push(...response.urls);
                } else if (response.url) {
                    uploadedImages.push(response.url);
                }
                document.getElementById("Thumbnail").value = JSON.stringify(uploadedImages);
                document.getElementById("mainForm").submit();
            });

            this.on("error", function (file, message) {
                console.error("Upload failed:", message);
            });
            this.on("removedfile", function (file) {
                let url = file.xhr ? JSON.parse(file.xhr.response).url : file.existingUrl || file.dataUrl;

                if (!url) return;

                const index = uploadedImages.indexOf(url);
                if (index > -1) {
                    uploadedImages.splice(index, 1);
                    document.getElementById("Thumbnail").value = JSON.stringify(uploadedImages);
                }
            });
        }
    });
}
//end dropzone

//search
const inputSearch = document.querySelector("[input-search]");
if (inputSearch) {
    let url = new URL(location.href);
    inputSearch.addEventListener("keyup", () => {
        const keyword = inputSearch.value.trim();

        url.searchParams.set("keyword", keyword);
        url.searchParams.set("page", 1);

        if (!keyword) {
            url.searchParams.delete("keyword");
        }

        history.replaceState(null, "", url.toString());

        fetch(url)
            .then(res => res.text())
            .then(html => {
                const newList = $(html).find("#product-list").html();

                document.getElementById("product-list").innerHTML = newList;
            })
    })

    //display default
    const keywordCurrent = url.searchParams.get("keyword");
    if (keywordCurrent) {
        inputSearch.value = keywordCurrent;
    }
    //end display default
}
//end search


//paginationv2
const listButtonPagination = document.querySelectorAll(".box-page.pro [button-pagination]");
if (listButtonPagination.length > 0) {
    let url = new URL(location.href);
    listButtonPagination.forEach(button => {
        button.addEventListener("click", (event) => {
            event.preventDefault();

            const page = button.getAttribute("button-pagination");
            if (page) {
                url.searchParams.set("page", page);
            } else {
                url.searchParams.delete("page");
            }

            location.href = url.href
        })
    })
}
// end paginationv2

//filter
const boxFilter = $("[box-filter]");
if (boxFilter.length > 0) {
    boxFilter.on("change", function () {
        let url = new URL(location.href);
        const value = boxFilter.val();
        console.log(url);
        if (value) {
            url.searchParams.set("status", value);
        } else {
            url.searchParams.delete("status");
        }

        location.href = url.href;
    });

    //display default
    const statusCurrent = new URL(location.href).searchParams.get("status");
    if (statusCurrent) {
        boxFilter.val(statusCurrent);
    }
    //end display default
}
//end filter

//sort
const sortSelect = $("[sort-select]");
if (sortSelect.length > 0) {
    sortSelect.on("change", function () {
        let url = new URL(location.href);
        const value = sortSelect.val();

        if (value) {
            const [sortKey, sortValue] = value.split("-");
            url.searchParams.set("sortKey", sortKey);
            url.searchParams.set("sortValue", sortValue);
        } else {
            url.searchParams.delete("sortKey");
            url.searchParams.delete("sortValue");
        }

        location.href = url.toString();
    });

    //display default
    const sortKeyCurrent = new URL(location.href).searchParams.get("sortKey");
    const sortValueCurrent = new URL(location.href).searchParams.get("sortValue");
    if (sortKeyCurrent && sortValueCurrent) {
        sortSelect.val(`${sortKeyCurrent}-${sortValueCurrent}`);
    }
    //end display default
}
//end sort

//changemulti
const formChangeMulti = document.querySelector("[form-change-multi]");
if (formChangeMulti) {
    formChangeMulti.addEventListener("submit", (event) => {
        event.preventDefault();

        const patch = formChangeMulti.getAttribute("data-patch");
        const status = formChangeMulti.status.value;

        if (status == "delete") {
            Swal.fire({
                title: "Are you sure you want to delete this record?",
                text: "",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#3085d6",
                cancelButtonColor: "#d33",
                confirmButtonText: "Yes, delete it!",
                cancelButtonText: "Cancel",
            }).then((result) => {
                if (!result.isConfirmed) {
                    return;
                }
                submitChangeMulti();
            });
        } else {
            submitChangeMulti();
        }

        function submitChangeMulti() {
            const ids = [];

            const listInputChange = document.querySelectorAll("[input-change]:checked");
            console.log(listInputChange)
            listInputChange.forEach(input => {
                const id = input.getAttribute("input-change");
                ids.push(id);
            });

            const data = {
                id: ids,
                status: status
            };

            fetch(patch, {
                headers: {
                    "Content-Type": "application/json",
                },
                method: "POST",
                body: JSON.stringify(data)
            })
                .then(res => res.json())
                .then(data => {
                    if (data.code == "deleted") {
                        Swal.fire({
                            title: "Deleted!",
                            text: "",
                            icon: "success",
                            timer: 1500,
                            showConfirmButton: false
                        }).then(() => {
                            location.reload();
                        });
                    } else {
                        location.reload();
                    }
                })
        }
    })
}
//end changemulti

//change position
const productListChangePosititon = document.getElementById("product-list");
if (productListChangePosititon) {
    productListChangePosititon.addEventListener("change", (e) => {
        const inputPosition = e.target.closest("[input-position]");

        if (inputPosition) {
            const value = parseInt(inputPosition.value)
            const id = parseInt(inputPosition.getAttribute("item-id"));
            const patch = inputPosition.getAttribute("data-patch");

            fetch(patch, {
                headers: {
                    "Content-Type": "application/json",
                },
                method: "POST",
                body: JSON.stringify({
                    id: id,
                    position: value
                })
            })
                .then(res => res.json())
                .then(data => {
                    if (data.code == "success") {
                        location.reload();
                    }
                })
        }
    })
}
//end change position

//permission
const tablePermission = document.querySelector("[table-permission]");
if (tablePermission) {
    const buttonSubmit = document.querySelector("[button-submit]");
    if (buttonSubmit) {
        buttonSubmit.addEventListener("click", () => {
            const data = [];

            const listElementRoleId = document.querySelectorAll("[role-id]");
            listElementRoleId.forEach(elementRoleId => {
                const roleId = elementRoleId.getAttribute("role-id");
                const permission = [];
                const listInputChecked = document.querySelectorAll(`input[data-id="${roleId}"]:checked`);

                listInputChecked.forEach(input => {
                    const tr = input.closest("tr[data-name]");
                    const name = tr.getAttribute("data-name");

                    permission.push(name);

                });

                data.push({
                    id: roleId,
                    permission: permission
                });
            });

            const patch = buttonSubmit.getAttribute("data-patch");
            fetch(patch, {
                headers: {
                    "Content-Type": "application/json"
                },
                method: "POST",
                body: JSON.stringify(data)
            })
                .then(res => res.json())
                .then(data => {
                    if (data.code == "success") {
                        location.reload();
                    }
                })
        });
    }
    //display default
    let dataPermission = tablePermission.getAttribute("table-permission");
    dataPermission = JSON.parse(dataPermission)
    dataPermission.forEach(item => {
        item.permissions.forEach(permission => {
            const input = document.querySelector(`tr[data-name="${permission}"] input[data-id="${item.id}"]`);
            input.checked = true;
        })
    })
    //end display default
}
//end permission

//search All user
$(document).ready(function () {
    $("#searchInput").on("keyup", function () {
        var value = $(this).val().toLowerCase();
        $("#userTableBody tr").filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1)
        });
    });
});
//end search All user

//show user detail 
$(document).ready(function () {
    $('#userDetailModal').on('show.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        const userId = button.data('user-id');
        const contentDiv = $('#userDetailContent');

        // Hiển thị loading
        contentDiv.html(`
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
            </div>
        `);
        $.ajax({
            url: '/admin/user/get-user-detail',
            type: 'GET',
            data: { id: userId },
            success: function (responseHtml) {
                contentDiv.html(responseHtml);
            },
            error: function () {
                Swal.fire({
                    icon: "error",
                    title: "Error loading data",
                    text: res.message
                });
            }
        });
    });
});
//end show user detail

//show user edit 
$(document).ready(function () {
    $('#userEditModal').on('show.bs.modal', function (event) {
        const button = $(event.relatedTarget);
        const userId = button.data('user-id');
        const contentDiv = $('#userEditContent');

        // Hiển thị loading
        contentDiv.html(`
            <div class="text-center py-4">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
            </div>
        `);

        $.ajax({
            url: '/admin/user/get-user-edit',
            type: 'GET',
            data: { id: userId },
            success: function (responseHtml) {
                contentDiv.html(responseHtml);
            },
            error: function () {
                Swal.fire({
                    icon: "error",
                    title: "Error loading data",
                    text: "Unable to load edit form"
                });
            }
        });
    });

    // Xử lý submit form edit
    $(document).on('submit', '#userEditForm', function (e) {
        e.preventDefault();

        const formData = new FormData(this);

        $.ajax({
            url: '/admin/user/update',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        icon: "success",
                        title: "Success",
                        text: response.message || "Information updated successfully"
                    }).then(() => {
                        $('#userEditModal').modal('hide');
                        location.reload(); // Reload trang để cập nhật dữ liệu
                    });
                } else {
                    Swal.fire({
                        icon: "error",
                        title: "Error",
                        text: response.message || "An error occurred"
                    });
                }
            },
            error: function () {
                Swal.fire({
                    icon: "error",
                    title: "Error",
                    text: "Unable to update information"
                });
            }
        });
    });
});
//end show user edit


//My profile (admin)
function viewMyProfile(userId) {
    $.ajax({
        url: '/admin/user/get-user-detail',
        type: 'GET',
        data: { id: userId },
        success: function (response) {
            $('#myProfileContent').html(response);
            $('#myProfileModal').modal('show');
        },
        error: function (xhr, status, error) {
            console.error('Error loading profile:', error);
            Swal.fire({
                icon: "error",
                title: "Error loading data",
                text: "Unable to load personal information"
            });
        }
    });
}
//End my profile (admin)    
//Profile Settings
$(document).ready(function () {
    const profileForm = $('#profileUpdateForm');

    if (profileForm.length > 0) {
        // Real-time password validation
        $('#password, #confirmPassword').on('input', function () {
            const password = $('#password').val();
            const confirmPassword = $('#confirmPassword').val();

            if (password && confirmPassword) {
                if (password !== confirmPassword) {
                    $('#confirmPassword').addClass('is-invalid');
                    const errorSpan = $('#confirmPassword').next('.text-danger');
                    if (errorSpan.length) {
                        errorSpan.text('Confirm password does not match');
                    }
                } else {
                    $('#confirmPassword').removeClass('is-invalid');
                    const errorSpan = $('#confirmPassword').next('.text-danger');
                    if (errorSpan.length) {
                        errorSpan.text('');
                    }
                }
            }
        });

        // Validate phone number (chỉ cho phép số và bắt đầu bằng 0)
        $('input[name="PhoneNumber"]').on('input', function () {
            let value = $(this).val();
            // Chỉ cho phép số
            value = value.replace(/[^0-9]/g, '');
            // Giới hạn 10 ký tự
            if (value.length > 10) {
                value = value.substring(0, 10);
            }
            $(this).val(value);
        });

        // Validate email real-time
        $('input[name="Email"]').on('blur', function () {
            const email = $(this).val();
            const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
            const errorSpan = $(this).next('.text-danger');

            if (email && !emailRegex.test(email)) {
                $(this).addClass('is-invalid');
                if (errorSpan.length) {
                    errorSpan.text('Email is not in correct format.');
                }
            } else {
                $(this).removeClass('is-invalid');
                if (errorSpan.length) {
                    errorSpan.text('');
                }
            }
        });

        // Preview uploaded photo
        $('input[name="Photo"]').on('change', function (e) {
            const file = e.target.files[0];
            if (file) {
                // Kiểm tra kích thước file (max 5MB)
                if (file.size > 5 * 1024 * 1024) {
                    Swal.fire({
                        icon: "warning",
                        title: "File is too large",
                        text: "File size must not exceed 5MB"
                    });
                    $(this).val('');
                    return;
                }

                // Kiểm tra loại file
                const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
                if (!allowedTypes.includes(file.type)) {
                    Swal.fire({
                        icon: "warning",
                        title: "Invalid format",
                        text: "Only image files (JPG, PNG, GIF) are accepted."
                    });
                    $(this).val('');
                    return;
                }
            }
        });

        // Clear password fields khi click vào password
        $('#password').on('focus', function () {
            if (!$(this).val()) {
                $('#confirmPassword').val('');
                $('#confirmPassword').removeClass('is-invalid');
            }
        });

        // Profile Update Form Submit Handler
        profileForm.on('submit', function (e) {
            e.preventDefault();

            const password = $('#password').val();
            const confirmPassword = $('#confirmPassword').val();

            // Validate password nếu có nhập
            if (password && password.length > 0) {
                // Bắt buộc phải có confirm password
                if (!confirmPassword || confirmPassword.length === 0) {
                    Swal.fire({
                        icon: "warning",
                        title: "Missing password confirmation",
                        text: "Please enter password confirmation when changing password."
                    });
                    return false;
                }

                // Kiểm tra khớp
                if (password !== confirmPassword) {
                    Swal.fire({
                        icon: "warning",
                        title: "Passwords do not match",
                        text: "Password and confirm password must be the same."
                    });
                    return false;
                }

                // Kiểm tra độ mạnh của password
                const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{6,}$/;
                if (!passwordRegex.test(password)) {
                    Swal.fire({
                        icon: "warning",
                        title: "Password is not strong enough",
                        text: "Password must be at least 6 characters, including 1 uppercase letter, 1 lowercase letter, 1 number and 1 special character."
                    });
                    return false;
                }
            }

            // Show loading
            Swal.fire({
                title: 'Updating...',
                text: 'Please wait a moment.',
                allowOutsideClick: false,
                didOpen: () => {
                    Swal.showLoading();
                }
            });

            // Submit form
            const formData = new FormData(this);

            // Debug: Kiểm tra URL
            const formAction = $(this).attr('action') || '/admin/setting/profile';
            console.log('Form action URL:', formAction);

            $.ajax({
                url: formAction,
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false,
                success: function (response) {
                    Swal.close();
                    if (response.success) {
                        Swal.fire({
                            icon: "success",
                            title: "Success",
                            text: response.message || "Information updated successfully!",
                            timer: 2000,
                            showConfirmButton: false
                        }).then(() => {
                            location.reload();
                        });
                    } else {
                        Swal.fire({
                            icon: "error",
                            title: "Error",
                            text: response.message || "Update failed"
                        });
                    }
                },
                error: function (xhr, status, error) {
                    Swal.close();
                    console.error('Update profile error:', xhr);
                    console.error('Status:', xhr.status);
                    console.error('Response:', xhr.responseText);

                    let errorMessage = "The information could not be updated. Please try again.";

                    // Parse error từ server
                    try {
                        const errorResponse = JSON.parse(xhr.responseText);
                        if (errorResponse.message) {
                            errorMessage = errorResponse.message;
                        }
                    } catch (e) {
                        console.log('Could not parse error response');
                    }

                    Swal.fire({
                        icon: "error",
                        title: "Connection error",
                        text: errorMessage
                    });
                }
            });

            return false;
        });
    }
});
//end Profile Settings

//Safety Stock
const inputStockQuantity = document.getElementById("StockQuantity");
if (inputStockQuantity) {
    function safetyStock() {
        const value = parseInt(inputStockQuantity.value) || 0;
        const selectStockStatus = $("[stock-status]");;

        let option;
        if (value === 0) {
            option = [...selectStockStatus.find("option")].find(opt => opt.text.trim() === "Hết hàng");
        } else if (value <= 10) {
            option = [...selectStockStatus.find("option")].find(opt => opt.text.trim() === "Sắp hết hàng");
        } else {
            option = [...selectStockStatus.find("option")].find(opt => opt.text.trim() === "Còn hàng");
        }

        if (option) {
            option.selected = true;
            selectStockStatus.find(`option[value="${option.value}"]`).prop("disabled", false);
            selectStockStatus.find("option").not(`[value="${option.value}"]`).prop("disabled", true);
            selectStockStatus.val(option.value).trigger('change');
        }

        //selectStockStatus.dispatchEvent(new Event("change"));
    }
    safetyStock()

    $("#StockQuantity").on("change", safetyStock);
}
//end Safety Stock


