/**
 * 控制台日志输出工具
 */

/**
 * 格式化时间戳
 * @returns {string} 格式化的时间字符串
 */
function formatTimestamp() {
    const now = new Date();
    const year = now.getFullYear();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const seconds = String(now.getSeconds()).padStart(2, '0');
    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
}

/**
 * 输出成功日志
 * @param {string} message - 日志消息
 */
export function outPutSuccess(message) {
    const timestamp = formatTimestamp();
    console.log(
        `%c ✓ Success %c [${timestamp}] %c ${message}`,
        'color: #ffffff; background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 4px 10px; border-radius: 4px 0 0 4px; font-weight: 600; letter-spacing: 0.3px;',
        'color: #a7f3d0; background: rgba(16, 185, 129, 0.15); padding: 4px 10px; font-weight: 500; border-left: 1px solid rgba(16, 185, 129, 0.3); border-right: 1px solid rgba(16, 185, 129, 0.3);',
        'color: #6ee7b7; background: rgba(16, 185, 129, 0.08); padding: 4px 10px; border-radius: 0 4px 4px 0; font-weight: 400;'
    );
}

/**
 * 输出信息日志
 * @param {string} message - 日志消息
 */
export function outPutInfo(message) {
    const timestamp = formatTimestamp();
    console.log(
        `%c ℹ Info %c [${timestamp}] %c ${message}`,
        'color: #ffffff; background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); padding: 4px 10px; border-radius: 4px 0 0 4px; font-weight: 600; letter-spacing: 0.3px;',
        'color: #93c5fd; background: rgba(59, 130, 246, 0.15); padding: 4px 10px; font-weight: 500; border-left: 1px solid rgba(59, 130, 246, 0.3); border-right: 1px solid rgba(59, 130, 246, 0.3);',
        'color: #bfdbfe; background: rgba(59, 130, 246, 0.08); padding: 4px 10px; border-radius: 0 4px 4px 0; font-weight: 400;'
    );
}

/**
 * 输出警告日志
 * @param {string} message - 日志消息
 */
export function outPutWarning(message) {
    const timestamp = formatTimestamp();
    console.warn(
        `%c ⚠ Warning %c [${timestamp}] %c ${message}`,
        'color: #ffffff; background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%); padding: 4px 10px; border-radius: 4px 0 0 4px; font-weight: 600; letter-spacing: 0.3px;',
        'color: #fcd34d; background: rgba(245, 158, 11, 0.15); padding: 4px 10px; font-weight: 500; border-left: 1px solid rgba(245, 158, 11, 0.3); border-right: 1px solid rgba(245, 158, 11, 0.3);',
        'color: #fde68a; background: rgba(245, 158, 11, 0.08); padding: 4px 10px; border-radius: 0 4px 4px 0; font-weight: 400;'
    );
}

/**
 * 输出错误日志
 * @param {string} message - 日志消息
 */
export function outPutError(message) {
    const timestamp = formatTimestamp();
    console.error(
        `%c ✗ Error %c [${timestamp}] %c ${message}`,
        'color: #ffffff; background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); padding: 4px 10px; border-radius: 4px 0 0 4px; font-weight: 600; letter-spacing: 0.3px;',
        'color: #fca5a5; background: rgba(239, 68, 68, 0.15); padding: 4px 10px; font-weight: 500; border-left: 1px solid rgba(239, 68, 68, 0.3); border-right: 1px solid rgba(239, 68, 68, 0.3);',
        'color: #fecaca; background: rgba(239, 68, 68, 0.08); padding: 4px 10px; border-radius: 0 4px 4px 0; font-weight: 400;'
    );
}

