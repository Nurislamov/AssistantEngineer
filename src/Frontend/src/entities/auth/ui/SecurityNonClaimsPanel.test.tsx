import { render, screen } from "@testing-library/react";
import { SecurityNonClaimsPanel } from "./SecurityNonClaimsPanel";

describe("SecurityNonClaimsPanel", () => {
  it("renders required non-claims", () => {
    render(<SecurityNonClaimsPanel />);

    expect(screen.getByText(/No production security certification claim\./i)).toBeInTheDocument();
    expect(screen.getByText(/No SOC 2 \/ ISO 27001 compliance claim\./i)).toBeInTheDocument();
    expect(screen.getByText(/No full multi-tenant isolation claim yet\./i)).toBeInTheDocument();
    expect(screen.getByText(/No external identity provider integration claim\./i)).toBeInTheDocument();
    expect(screen.getByText(/No certified\/certification claim\./i)).toBeInTheDocument();
  });
});
