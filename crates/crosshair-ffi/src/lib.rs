use std::ffi::{CStr, CString};
use std::os::raw::{c_char, c_int};
use std::panic::{AssertUnwindSafe, catch_unwind};

use crosshair_core::{
    CrosshairData, ValidatedCrosshair, build_draw_cmds, decode_share_code, encode_share_code,
};
use crosshair_ipc::{Request, Response};

const CHZ_OK: c_int = 0;
const CHZ_ERR_NULL: c_int = 1;
const CHZ_ERR_UTF8: c_int = 2;
const CHZ_ERR_JSON: c_int = 3;
const CHZ_ERR_INTERNAL: c_int = 255;

#[unsafe(no_mangle)]
pub extern "C" fn chz_handle_request(
    input_json: *const c_char,
    output_json: *mut *mut c_char,
    error_json: *mut *mut c_char,
) -> c_int {
    catch_unwind(AssertUnwindSafe(|| {
        handle_request_inner(input_json, output_json, error_json)
    }))
    .unwrap_or_else(|_| {
        write_error(error_json, "internal panic in rust FFI boundary");
        CHZ_ERR_INTERNAL
    })
}

#[unsafe(no_mangle)]
pub extern "C" fn chz_free_string(ptr: *mut c_char) {
    if ptr.is_null() {
        return;
    }
    unsafe { drop(CString::from_raw(ptr)) }
}

fn handle_request_inner(
    input_json: *const c_char,
    output_json: *mut *mut c_char,
    error_json: *mut *mut c_char,
) -> c_int {
    if input_json.is_null() || output_json.is_null() || error_json.is_null() {
        write_error(error_json, "received null pointer");
        return CHZ_ERR_NULL;
    }

    unsafe {
        *output_json = std::ptr::null_mut();
        *error_json = std::ptr::null_mut();
    }

    let input = match c_str_to_str(input_json) {
        Ok(v) => v,
        Err(msg) => {
            write_error(error_json, msg);
            return CHZ_ERR_UTF8;
        }
    };

    let request: Request = match serde_json::from_str(input) {
        Ok(v) => v,
        Err(err) => {
            write_error(error_json, &format!("failed to parse request JSON: {err}"));
            return CHZ_ERR_JSON;
        }
    };

    let response = match dispatch(request) {
        Ok(v) => v,
        Err(msg) => {
            write_error(error_json, &msg);
            return CHZ_ERR_JSON;
        }
    };

    let response_json = match serde_json::to_string(&response) {
        Ok(v) => v,
        Err(err) => {
            write_error(
                error_json,
                &format!("failed to serialize response JSON: {err}"),
            );
            return CHZ_ERR_INTERNAL;
        }
    };

    unsafe {
        *output_json = into_c_string_ptr(&response_json);
    }

    CHZ_OK
}

fn dispatch(request: Request) -> Result<Response, String> {
    match request {
        Request::GetActiveProfile => Ok(Response::ActiveProfile(CrosshairData::default())),
        Request::SetActiveCrosshair(data) => {
            let validated = ValidatedCrosshair::new(data).map_err(|e| e.to_string())?;
            Ok(Response::ActiveProfile(validated.into_inner()))
        }
        Request::BuildPreview {
            crosshair,
            width,
            height,
        } => {
            let validated = ValidatedCrosshair::new(crosshair).map_err(|e| e.to_string())?;
            let cmds = build_draw_cmds(&validated, width / 2.0, height / 2.0);
            Ok(Response::Preview(cmds))
        }
        Request::EncodeShareCode(data) => {
            let code = encode_share_code(&data).map_err(|e| e.to_string())?;
            Ok(Response::ShareCode(code))
        }
        Request::DecodeShareCode(code) => {
            let data = decode_share_code(&code).map_err(|e| e.to_string())?;
            Ok(Response::Decoded(data))
        }
    }
}

fn c_str_to_str<'a>(ptr: *const c_char) -> Result<&'a str, &'static str> {
    let cstr = unsafe { CStr::from_ptr(ptr) };
    cstr.to_str().map_err(|_| "input was not valid UTF-8")
}

fn into_c_string_ptr(value: &str) -> *mut c_char {
    CString::new(value)
        .expect("CString::new failed due to interior NUL")
        .into_raw()
}

fn write_error(error_json: *mut *mut c_char, message: &str) {
    if error_json.is_null() {
        return;
    }

    let escaped = serde_json::json!({ "error": message }).to_string();
    unsafe {
        *error_json = into_c_string_ptr(&escaped);
    }
}
