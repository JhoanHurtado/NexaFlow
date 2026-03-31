"""
Envelope de respuesta estándar — mismo contrato que los servicios .NET.
Estructura: { success, data, errorCode, message }
"""
from typing import Any, Generic, TypeVar
from pydantic import BaseModel

T = TypeVar("T")


class ApiResponse(BaseModel, Generic[T]):
    success: bool
    data: T | None = None
    error_code: str | None = None
    message: str = ""

    @classmethod
    def ok(cls, data: Any, message: str = "") -> "ApiResponse":
        return cls(success=True, data=data, message=message)

    @classmethod
    def fail(cls, error_code: str, message: str) -> "ApiResponse":
        return cls(success=False, error_code=error_code, message=message)
