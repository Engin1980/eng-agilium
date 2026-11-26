import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { TemplateItemType } from "../../services/templates-api";
import { AttributeInput } from "./attribute-input";

describe("AttributeInput", () => {
  it("renders LabelOnly as plain text without any input", () => {
    render(<AttributeInput title="Just a label" type={TemplateItemType.LabelOnly} value={null} onChange={() => {}} />);

    expect(screen.getByText("Just a label")).toBeInTheDocument();
    expect(screen.queryByRole("textbox")).toBeNull();
  });

  it("reports an empty text as null", async () => {
    const onChange = vi.fn();
    render(<AttributeInput title="Name" type={TemplateItemType.SingleLineText} value="abc" onChange={onChange} />);

    await userEvent.clear(screen.getByRole("textbox"));

    expect(onChange).toHaveBeenLastCalledWith(null);
  });

  it("uses a number input for integers and a text input (comma allowed) for decimals", () => {
    const { rerender } = render(
      <AttributeInput title="Count" type={TemplateItemType.Integer} value="3" onChange={() => {}} />,
    );
    expect(screen.getByRole("spinbutton")).toHaveValue(3);

    rerender(<AttributeInput title="Hours" type={TemplateItemType.Decimal} value="1,5" onChange={() => {}} />);
    expect(screen.getByRole("textbox")).toHaveValue("1,5");
  });

  it("stores a checkbox as the strings 'true' / 'false'", async () => {
    const onChange = vi.fn();
    render(<AttributeInput title="Done" type={TemplateItemType.Boolean} value="false" onChange={onChange} />);

    await userEvent.click(screen.getByRole("checkbox"));

    expect(onChange).toHaveBeenCalledWith("true");
  });

  it("disables the control when disabled", () => {
    render(<AttributeInput title="Name" type={TemplateItemType.SingleLineText} value="" onChange={() => {}} disabled />);

    expect(screen.getByRole("textbox")).toBeDisabled();
  });
});
